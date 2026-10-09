using DuongNhan.Web.Components;
using DuongNhan.Web.Extensions;
using DuongNhan.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
builder.Services.AddSingleton<ProductExcelParser>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<PlanCatalogService>();
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddScoped<UiSessionService>();
builder.Services.AddScoped<AuthModalService>();
builder.Services.AddScoped<DoctorCatalogService>();

builder.Services.AddApiClients(builder.Configuration);

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
