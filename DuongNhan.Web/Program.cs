using DuongNhan.Web.Components;
using DuongNhan.Web.Extensions;
using DuongNhan.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

// Nạp .env (tìm ngược lên thư mục gốc). NoClobber: biến đã có (Aspire/Railway) không bị ghi đè.
DotNetEnv.Env.NoClobber().TraversePath().Load();

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

// Explicitly allow this origin to use the camera (live skin scan). Without this some hosts/proxies
// default to a restrictive policy and getUserMedia is silently blocked.
app.Use(async (context, next) =>
{
    context.Response.Headers["Permissions-Policy"] = "camera=(self)";
    await next();
});

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

// SePay calls this public URL (https://<web-domain>/webhooks/sepay) when money arrives. The API service is only
// reachable on Railway's private network, so the call is forwarded server-side. The API checks the
// "Authorization: Apikey ..." header, so it is passed through untouched.
app.MapPost("/webhooks/sepay", async (HttpContext context, IHttpClientFactory clients) =>
{
    var ct = context.RequestAborted;

    using var forward = new HttpRequestMessage(HttpMethod.Post, "/api/payments/sepay-webhook")
    {
        Content = new StreamContent(context.Request.Body)
    };
    forward.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

    if (context.Request.Headers.TryGetValue("Authorization", out var authorization))
        forward.Headers.TryAddWithoutValidation("Authorization", authorization.ToString());

    using var response = await clients.CreateClient("dn-api").SendAsync(forward, ct);

    context.Response.StatusCode = (int)response.StatusCode;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(await response.Content.ReadAsStringAsync(ct), ct);
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
