using System.Security.Claims;

namespace StudyFlow.Services;

/// <summary>
/// Curator APIs. Both ASP.NET authorization and a fresh database role check
/// are required. No endpoint grants roles or changes other users' data.
/// </summary>
public static class StudyFlowCuratorEndpoints
{
    public static void MapStudyFlowCuratorApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/curator")
            .RequireAuthorization("CuratorOnly");

        group.MapGet("/students", async (
            HttpContext context,
            SessionStore sessions, SupabaseAuth auth,
            StudyFlowRoleService roles, StudyFlowCuratorRepository repository) =>
        {
            var jwt = await AuthorizedTokenAsync(context, sessions, auth, roles);
            if (jwt is null) return Results.Forbid();
            if (!Guid.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                out var curatorId)) return Results.Forbid();
            return Results.Ok(await repository.GetStudentsAsync(jwt, curatorId));
        });

        group.MapGet("/students/{studentId:guid}/tasks", async (
            Guid studentId, HttpContext context,
            SessionStore sessions, SupabaseAuth auth,
            StudyFlowRoleService roles, StudyFlowCuratorRepository repository) =>
        {
            var jwt = await AuthorizedTokenAsync(context, sessions, auth, roles);
            if (jwt is null) return Results.Forbid();
            return Results.Ok(await repository.GetStudentTasksAsync(jwt, studentId));
        });

        group.MapGet("/students/{studentId:guid}/exams", async (
            Guid studentId, HttpContext context,
            SessionStore sessions, SupabaseAuth auth,
            StudyFlowRoleService roles, StudyFlowCuratorRepository repository) =>
        {
            var jwt = await AuthorizedTokenAsync(context, sessions, auth, roles);
            if (jwt is null) return Results.Forbid();
            return Results.Ok(await repository.GetStudentExamsAsync(jwt, studentId));
        });

        group.MapGet("/students/{studentId:guid}/scores", async (
            Guid studentId, HttpContext context,
            SessionStore sessions, SupabaseAuth auth,
            StudyFlowRoleService roles, StudyFlowCuratorRepository repository) =>
        {
            var jwt = await AuthorizedTokenAsync(context, sessions, auth, roles);
            if (jwt is null) return Results.Forbid();
            return Results.Ok(await repository.GetStudentScoresAsync(jwt, studentId));
        });
    }

    private static async Task<string?> AuthorizedTokenAsync(
        HttpContext context, SessionStore sessions,
        SupabaseAuth auth, StudyFlowRoleService roles)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var sessionId = context.User.FindFirst(SessionStore.Claim)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var jwt = await sessions.AccessToken(sessionId, auth);
        if (string.IsNullOrEmpty(jwt)) return null;

        // A Curator claim alone is not enough. Admin can revoke the database
        // role; this check takes effect without waiting for cookie expiration.
        return await roles.GetRoleAsync(jwt, userId) == StudyFlowRoleService.Curator
            ? jwt
            : null;
    }
}
