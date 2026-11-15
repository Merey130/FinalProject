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
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.Name = "StudyFlow.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Events.OnRedirectToAccessDenied = context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = 403;
    }
    else
    {
        context.Response.Redirect(context.RedirectUri);
    }

    return Task.CompletedTask;
};
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
builder.Services.AddAuthorization(options =>
    options.AddPolicy("CuratorOnly", policy => policy.RequireRole("Curator")));
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
builder.Services.AddScoped<StudyFlowRoleService>();
builder.Services.AddScoped<StudyFlowCuratorRepository>();
builder.Services.AddScoped<TaskState>();
builder.Services.AddScoped<MockExamState>();

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
app.MapStudyFlowCuratorApi();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
