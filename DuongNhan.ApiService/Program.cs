using System.Text;
using System.Threading.RateLimiting;
using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Features.Auth.Shared;
using DuongNhan.ApiService.Mappers;
using DuongNhan.ApiService.Models;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using FastEndpoints;
using HaveIBeenPwned.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<AppDbContext>("postgresdb", configureDbContextOptions: options =>
{
    options.UseNpgsql(npgsql =>
    {
        npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
        npgsql.CommandTimeout(30);
        npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

// ── Time + password hashing ────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.Configure<PasswordHasherOptions>(options =>
{
    options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
    options.IterationCount = 210_000;
});

// ── HaveIBeenPwned k-anonymity client ──────────────────────────
// Note: HaveIBeenPwned.Client 10.0.1 does NOT support padding.
// Only UserAgent and ApiKey are available on HibpOptions.
builder.Services.AddPwnedServices(options =>
{
    options.UserAgent = builder.Configuration["HaveIBeenPwned:UserAgent"]
        ?? "DuongNhan-SkinAnalysis/1.0";
});

// ── Application services ───────────────────────────────────────
builder.Services.AddScoped<BreachedPasswordValidator>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<DummyPasswordVerifier>();
builder.Services.AddScoped<ILoginAttemptTracker, LoginAttemptTracker>();
builder.Services.AddScoped<ITokenInvalidationCache, TokenInvalidationCache>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<UserMapper>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDiagnosisService, OpenAiDiagnosisService>();

// ── JWT signing key ────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);
if (jwtKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 bytes (256 bits) to safely sign HS256 tokens.");
}

// ── JWT authentication ─────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
            ClockSkew = TimeSpan.FromSeconds(
                builder.Configuration.GetValue("Jwt:ClockSkewSeconds", 30))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirst(AppClaimTypes.UserId)?.Value;

                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Missing subject.");
                    return;
                }

                var invalidationCache = context.HttpContext.RequestServices
                    .GetRequiredService<ITokenInvalidationCache>();
                var state = await invalidationCache.GetAsync(userId, context.HttpContext.RequestAborted);

                if (!state.Found)
                {
                    context.Fail("Token subject no longer exists.");
                    return;
                }

                if (context.SecurityToken.ValidFrom < state.InvalidatedAt)
                    context.Fail("Token has been revoked.");
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.RequireUser, policy => policy.RequireAuthenticatedUser());

// ── Rate limiting ──────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var key = httpContext.User.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst(AppClaimTypes.UserId)?.Value ?? "anonymous"
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

// ── FastEndpoints / OpenAPI / errors ───────────────────────────
builder.Services.AddFastEndpoints(o =>
{
    o.IncludeAbstractValidators = true;
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Global response hardening: never let intermediaries or browsers cache auth
// payloads (they carry tokens) and always disable MIME sniffing.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";

        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            headers["Cache-Control"] = "no-store";
            headers["Pragma"] = "no-cache";
        }

        return Task.CompletedTask;
    });

    await next();
});

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    await AppDbSeeder.SeedAsync(db, timeProvider);

    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";
    config.Errors.UseProblemDetails();
    config.Errors.ResponseBuilder = (List<ValidationFailure> failures, HttpContext _, int statusCode) =>
    {
        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

        return new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            title = "One or more validation errors occurred.",
            status = statusCode,
            errors
        };
    };
});

app.MapDefaultEndpoints();

app.Run();