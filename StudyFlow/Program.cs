using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using StudyFlow.Components;
using StudyFlow.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.Name = "StudyFlow.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Events.OnValidatePrincipal = async context =>
        {
            var store = context.HttpContext.RequestServices.GetRequiredService<SessionStore>();
            if (!store.Exists(context.Principal?.FindFirst(SessionStore.Claim)?.Value))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpClient("supabase", c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddSingleton(new SupabaseOptions {
    Url = builder.Configuration["Supabase:Url"] ?? "",
    PublishableKey = builder.Configuration["Supabase:PublishableKey"] ?? ""
});
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<SupabaseAuth>();
builder.Services.AddSingleton<SupabaseDatabase>();
builder.Services.AddScoped<CurrentAccessToken>();
builder.Services.AddScoped<StudyRepository>();
builder.Services.AddScoped<TaskState>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapGet("/api/health", (SupabaseOptions o) =>
    Results.Ok(new { status = "ok", app = "StudyFlow", supabaseConfigured = o.Configured }));
app.MapStudyFlowAuth();
app.MapStudyFlowApi();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
