using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;

namespace StudyFlow.Services;

public static class AuthEndpoints
{
    public static void MapStudyFlowAuth(this WebApplication app)
    {
        app.MapPost("/auth/register", Register);
        app.MapPost("/auth/login", Login);
        app.MapPost("/auth/logout", Logout).RequireAuthorization();
    }

    private static async Task<IResult> Register(HttpContext c, IAntiforgery af, SupabaseAuth auth)
    {
        if (!await Valid(c, af)) return Results.BadRequest("Invalid antiforgery token.");
        var f = await c.Request.ReadFormAsync();
        var email = f["email"].ToString().Trim();
        var password = f["password"].ToString();
        var confirm = f["confirmPassword"].ToString();
        var name = f["displayName"].ToString().Trim();
        if (email.Length > 254 || !email.Contains('@') || password.Length < 8 ||
            password.Length > 72 || password != confirm || name.Length > 100)
            return Results.Redirect("/register?error=validation");
        try {
            var ok = await auth.Register(email, password, name);
            return Results.Redirect(ok ? "/login?registered=1" : "/register?error=failed");
        } catch { return Results.Redirect("/register?error=service"); }
    }

    private static async Task<IResult> Login(HttpContext c, IAntiforgery af, SupabaseAuth auth, SessionStore sessions)
    {
        if (!await Valid(c, af)) return Results.BadRequest("Invalid antiforgery token.");
        var f = await c.Request.ReadFormAsync();
        var email = f["email"].ToString().Trim();
        var password = f["password"].ToString();
        try {
            var a = await auth.Login(email, password);
            if (a is null || string.IsNullOrWhiteSpace(a.AccessToken) ||
                string.IsNullOrWhiteSpace(a.RefreshToken) || !Guid.TryParse(a.User.Id, out _))
                return Results.Redirect("/login?error=invalid");
            var sid = sessions.Add(a);
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, a.User.Id),
                new Claim(ClaimTypes.Email, a.User.Email ?? email),
                new Claim(SessionStore.Claim, sid)
            };
            await c.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                new AuthenticationProperties { IsPersistent = true });
            return Results.Redirect("/");
        } catch { return Results.Redirect("/login?error=service"); }
    }

    private static async Task<IResult> Logout(HttpContext c, IAntiforgery af, SupabaseAuth auth, SessionStore sessions)
    {
        if (!await Valid(c, af)) return Results.BadRequest("Invalid antiforgery token.");
        var sid = c.User.FindFirst(SessionStore.Claim)?.Value;
        try {
            var token = await sessions.AccessToken(sid, auth);
            if (token is not null) await auth.Logout(token);
        } catch { }
        sessions.Remove(sid);
        await c.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    private static async Task<bool> Valid(HttpContext c, IAntiforgery af)
    {
        try { await af.ValidateRequestAsync(c); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
