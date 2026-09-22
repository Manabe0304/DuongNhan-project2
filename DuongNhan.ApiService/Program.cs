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
    options.UserAgent = "DuongNhan-SkinAnalysis/1.0";
});

// ── Application services ───────────────────────────────────────
builder.Services.AddScoped<BreachedPasswordValidator>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ILoginAttemptTracker, LoginAttemptTracker>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<UserMapper>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDiagnosisService, OpenAiDiagnosisService>();

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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Jwt:Key is not configured."))),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var userIdClaim = context.Principal?.FindFirst(AppClaimTypes.UserId)?.Value;

                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Missing subject.");
                    return;
                }

                var issuedAt = context.SecurityToken.ValidFrom;
                var invalidatedAt = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => u.TokensInvalidatedAt)
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                if (issuedAt < invalidatedAt)
                    context.Fail("Token has been revoked.");
            }
        };
    });

builder.Services.AddAuthorization();

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
    config.Errors.ResponseBuilder = (failures, _, _) =>
    {
        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

        return new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            title = "One or more validation errors occurred.",
            status = StatusCodes.Status400BadRequest,
            errors
        };
    };
});

app.MapDefaultEndpoints();

app.Run();