using StudyFlow.Models;

namespace StudyFlow.Services;

public static class StudyFlowEndpoints
{
    public static void MapStudyFlowApi(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();

        api.MapGet("/me", (HttpContext c) => Results.Ok(new {
            id=c.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            email=c.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        }));

        api.MapGet("/tasks", async (HttpContext c, SessionStore s, SupabaseAuth a, StudyRepository r) =>
            Results.Ok(await r.GetTasks(await Token(c,s,a))));

        api.MapPost("/tasks", async (HttpContext c, NewTask x, SessionStore s, SupabaseAuth a, StudyRepository r) => {
            if (string.IsNullOrWhiteSpace(x.Title)||x.Title.Length>200||string.IsNullOrWhiteSpace(x.Subject)||x.Subject.Length>80||x.Deadline==default)
                return Results.BadRequest(new {error="Invalid task fields."});
            var row=await r.AddTask(await Token(c,s,a),x.Title.Trim(),x.Subject.Trim(),x.Deadline);
            return Results.Created($"/api/tasks/{row.Id}",row);
        });

        api.MapPatch("/tasks/{id:int}", async (HttpContext c, int id, TaskDone x, SessionStore s, SupabaseAuth a, StudyRepository r) => {
            var row=await r.SetDone(await Token(c,s,a),id,x.IsDone);
            return row is null?Results.NotFound():Results.Ok(row);
        });

        api.MapDelete("/tasks/{id:int}", async (HttpContext c, int id, SessionStore s, SupabaseAuth a, StudyRepository r) => {
            await r.DeleteTask(await Token(c,s,a),id); return Results.NoContent();
        });

        api.MapGet("/exams", async (HttpContext c, SessionStore s, SupabaseAuth a, StudyRepository r) =>
            Results.Ok(await r.GetExams(await Token(c,s,a))));

        api.MapPost("/exams", async (HttpContext c, NewExam x, SessionStore s, SupabaseAuth a, StudyRepository r) => {
            if (string.IsNullOrWhiteSpace(x.Title)||x.Title.Length>200||x.TakenOn==default||x.Subjects is null||x.Subjects.Count is <1 or >20||
                x.Subjects.Any(v=>string.IsNullOrWhiteSpace(v.Subject)||v.Subject.Length>80||v.MaxScore<1||v.Score<0||v.Score>v.MaxScore)||
                x.Subjects.Select(v=>v.Subject.Trim().ToLowerInvariant()).Distinct().Count()!=x.Subjects.Count)
                return Results.BadRequest(new {error="Invalid exam or subject scores."});
            var row=await r.AddExam(await Token(c,s,a),x.Title.Trim(),x.TakenOn,x.Subjects);
            return Results.Created($"/api/exams/{row.Id}",row);
        });

        api.MapDelete("/exams/{id:int}", async (HttpContext c, int id, SessionStore s, SupabaseAuth a, StudyRepository r) => {
            await r.DeleteExam(await Token(c,s,a),id); return Results.NoContent();
        });

        api.MapGet("/progress", async (HttpContext c, SessionStore s, SupabaseAuth a, StudyRepository r) => {
            var token=await Token(c,s,a);
            var tasks=await r.GetTasks(token); var exams=await r.GetExams(token);
            var done=tasks.Count(v=>v.IsDone);
            return Results.Ok(new {totalTasks=tasks.Count,completedTasks=done,pendingTasks=tasks.Count-done,
                completionPercent=tasks.Count==0?0:done*100/tasks.Count,examCount=exams.Count,recentExams=exams.Take(5)});
        });
    }

    private static async Task<string> Token(HttpContext c, SessionStore sessions, SupabaseAuth auth)
    {
        var sid=c.User.FindFirst(SessionStore.Claim)?.Value;
        return await sessions.AccessToken(sid,auth) ?? throw new UnauthorizedAccessException();
    }
}
public sealed record NewTask(string Title,string Subject,DateTime Deadline);
public sealed record TaskDone(bool IsDone);
public sealed record NewExam(string Title,DateTime TakenOn,List<SubjectMark>? Subjects);
