using Microsoft.AspNetCore.Routing;
using StudyFlow.Models;

namespace StudyFlow.Services;

// Development-only API for tasks, students and mock exam results.
// Auth must be implemented before this is used with real student data.
public static class StudyFlowEndpoints
{
    public static IEndpointRouteBuilder MapStudyFlowApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/health", () => Results.Ok(new { status = "ok", app = "StudyFlow" }));
        api.MapGet("/db-health", async (StudyRepository repo, ILogger<StudyRepository> log,
            CancellationToken ct) =>
        {
            try
            {
                return await repo.CanConnectAsync(ct)
                    ? Results.Ok(new { status = "connected" })
                    : Results.Problem("Database not available", statusCode: 503);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Supabase database connection failed");
                return Results.Problem("Database connection unavailable; check local server logs.", statusCode: 503);
            }
        });

        api.MapGet("/tasks", async (StudyRepository repo, CancellationToken ct) =>
            Results.Ok(await repo.GetTasksAsync(ct)));

        api.MapPost("/tasks", async (TaskInput input, StudyRepository repo, CancellationToken ct) =>
        {
            var error = ValidateTask(input);
            if (error is not null) return Results.BadRequest(new { error });
            var task = await repo.CreateTaskAsync(input.Title, input.Subject, input.Deadline, ct);
            return Results.Created($"/api/tasks/{task.Id}", task);
        });

        api.MapPut("/tasks/{id:int}", async (int id, TaskEditInput input,
            StudyRepository repo, CancellationToken ct) =>
        {
            var error = ValidateTask(input);
            if (error is not null) return Results.BadRequest(new { error });
            var task = await repo.UpdateTaskAsync(id, input.Title, input.Subject,
                input.Deadline, input.IsDone, ct);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        api.MapPatch("/tasks/{id:int}/toggle", async (int id, StudyRepository repo, CancellationToken ct) =>
            await repo.ToggleTaskAsync(id, ct) is null ? Results.NotFound() : Results.NoContent());

        api.MapDelete("/tasks/{id:int}", async (int id, StudyRepository repo, CancellationToken ct) =>
            await repo.DeleteTaskAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        api.MapGet("/progress", async (StudyRepository repo, CancellationToken ct) =>
        {
            var tasks = await repo.GetTasksAsync(ct);
            var done = tasks.Count(t => t.IsDone);
            return Results.Ok(new
            {
                totalTasks = tasks.Count,
                completedTasks = done,
                pendingTasks = tasks.Count - done,
                overdueTasks = tasks.Count(t => !t.IsDone && t.Deadline.Date < DateTime.Today),
                completionPercent = tasks.Count == 0 ? 0 : (int)Math.Round(done * 100.0 / tasks.Count)
            });
        });

        api.MapGet("/students", async (StudyRepository repo, CancellationToken ct) =>
            Results.Ok(await repo.GetStudentsAsync(ct)));

        api.MapGet("/students/{id:int}", async (int id, StudyRepository repo, CancellationToken ct) =>
        {
            var student = await repo.GetStudentAsync(id, ct);
            return student is null ? Results.NotFound() : Results.Ok(student);
        });

        api.MapPost("/students", async (StudentInput input, StudyRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 80)
                return Results.BadRequest(new { error = "Name must be 1–80 characters." });
            var student = await repo.CreateStudentAsync(input.Name, ct);
            return Results.Created($"/api/students/{student.Id}", student);
        });

        api.MapGet("/exams", async (int? studentId, StudyRepository repo, CancellationToken ct) =>
            Results.Ok(await repo.GetExamsAsync(studentId, ct)));

        api.MapPost("/exams", async (ExamInput input, StudyRepository repo, CancellationToken ct) =>
        {
            var error = ValidateExam(input);
            if (error is not null) return Results.BadRequest(new { error });
            var exam = await repo.CreateExamAsync(
                input.StudentId, input.Title, input.TakenOn, input.Subjects!, ct);
            return exam is null
                ? Results.NotFound(new { error = "Student not found. Create a student first." })
                : Results.Created($"/api/exams/{exam.Id}", exam);
        });

        api.MapDelete("/exams/{id:int}", async (int id, StudyRepository repo, CancellationToken ct) =>
            await repo.DeleteExamAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        api.MapGet("/students/{id:int}/progress", async (int id, StudyRepository repo,
            CancellationToken ct) =>
        {
            var student = await repo.GetStudentAsync(id, ct);
            if (student is null) return Results.NotFound();
            var exams = await repo.GetExamsAsync(id, ct);
            var latest = exams.FirstOrDefault();
            var best = exams.OrderByDescending(e => e.Percentage).FirstOrDefault();
            return Results.Ok(new
            {
                student,
                examCount = exams.Count,
                latestExam = latest,
                bestExam = best,
                latestSubjectScores = latest?.Subjects ?? new List<SubjectMark>()
            });
        });

        return app;
    }

    private static string? ValidateTask(TaskInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 200)
            return "Title must be 1–200 characters.";
        if (string.IsNullOrWhiteSpace(input.Subject) || input.Subject.Trim().Length > 80)
            return "Subject must be 1–80 characters.";
        if (input.Deadline == default) return "Deadline is required.";
        return null;
    }

    private static string? ValidateExam(ExamInput input)
    {
        if (input.StudentId < 1) return "Valid studentId is required.";
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 200)
            return "Exam title must be 1–200 characters.";
        if (input.TakenOn == default) return "TakenOn is required.";
        if (input.Subjects is null || input.Subjects.Count is < 1 or > 20)
            return "Provide 1–20 subjects.";
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in input.Subjects)
        {
            if (s is null || string.IsNullOrWhiteSpace(s.Subject) || s.Subject.Trim().Length > 80)
                return "Every subject must have a name (1–80 characters).";
            if (!names.Add(s.Subject.Trim())) return "Subject names must be unique per exam.";
            if (s.MaxScore is < 1 or > 1000 || s.Score < 0 || s.Score > s.MaxScore)
                return "Score must be 0..maxScore; maxScore must be 1..1000.";
        }
        return null;
    }

    public class TaskInput
    {
        public string Title { get; set; } = "";
        public string Subject { get; set; } = "Informatics";
        public DateTime Deadline { get; set; }
    }

    public sealed class TaskEditInput : TaskInput
    {
        public bool IsDone { get; set; }
    }

    public sealed class StudentInput
    {
        public string Name { get; set; } = "";
    }

    public sealed class ExamInput
    {
        public int StudentId { get; set; }
        public string Title { get; set; } = "";
        public DateTime TakenOn { get; set; }
        public List<SubjectMark>? Subjects { get; set; }
    }
}
