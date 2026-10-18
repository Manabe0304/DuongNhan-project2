using DuongNhan.Web.Components;
using DuongNhan.Web.Extensions;
using DuongNhan.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Aspire's default resilience handler times each call out after 10 s and retries it, POSTs included. A slow
// upload was therefore sent again while the first copy was still running, creating duplicate scans (and
// counting twice). Never retry unsafe methods, and allow a scan enough time to finish.
builder.Services.ConfigureAll<Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions>(options =>
{
    options.Retry.ShouldHandle = _ => ValueTask.FromResult(false);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(180);
    });


builder.Services.AddWebBlazor();

builder.Services.AddHttpContextAccessor();
builder.Services.AddWebAuthentication();

builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<NavigationService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ImageValidationService>();
builder.Services.AddScoped<DuongNhanApiService>();
builder.Services.AddScoped<AdminApiService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<UsageState>();
builder.Services.AddSingleton<ProductExcelParser>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<PlanCatalogService>();
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddScoped<UiSessionService>();
builder.Services.AddScoped<AuthModalService>();
builder.Services.AddScoped<DoctorCatalogService>();

builder.Services.AddApiClients(builder.Configuration);

// Plain client used by the /skin-image proxy below.
builder.Services.AddHttpClient("dn-api", c =>
    c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https+http://apiservice"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/404");
app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseOutputCache();
app.MapStaticAssets();

// Scan thumbnails: the API returns relative image paths (/api/skin/{id}), but the browser can only reach this
// web app, so the image is fetched server-side and streamed back.
app.MapGet("/skin-image/{id:guid}", async (Guid id, IHttpClientFactory clients, CancellationToken ct) =>
{
    using var response = await clients.CreateClient("dn-api")
        .GetAsync($"/api/skin/{id}", ct);

    if (!response.IsSuccessStatusCode) return Results.NotFound();

    var bytes = await response.Content.ReadAsByteArrayAsync(ct);
    return Results.File(bytes, response.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
