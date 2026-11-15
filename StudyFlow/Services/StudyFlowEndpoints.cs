using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StudyFlow.Models;

namespace StudyFlow.Services;

public static class StudyFlowEndpoints
{
    public static IEndpointRouteBuilder MapStudyFlowApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/health", () => Results.Ok(new { status = "ok", app = "StudyFlow" }));

        // Assignments: automatically reflected on the existing Blazor pages.
        api.MapGet("/tasks", (StudyDataStore store) => Results.Ok(store.GetTasks()));

        api.MapPost("/tasks", (TaskInput input, StudyDataStore store) =>
        {
            if (string.IsNullOrWhiteSpace(input.Title))
                return Results.BadRequest(new { error = "Title is required." });
            if (input.Title.Length > 200)
                return Results.BadRequest(new { error = "Title must be 200 characters or less." });
            if (input.Deadline == default)
                return Results.BadRequest(new { error = "Deadline is required." });

            var task = store.AddTask(input.Title, input.Subject, input.Deadline);
            return Results.Created($"/api/tasks/{task.Id}", task);
        });

        api.MapPut("/tasks/{id:int}", (int id, TaskEditInput input, StudyDataStore store) =>
        {
            if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 || input.Deadline == default)
                return Results.BadRequest(new { error = "Valid title and deadline are required." });

            var task = store.UpdateTask(id, input.Title, input.Subject, input.Deadline, input.IsDone);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        api.MapPatch("/tasks/{id:int}/toggle", (int id, StudyDataStore store) =>
            store.ToggleTask(id) ? Results.NoContent() : Results.NotFound());

        api.MapDelete("/tasks/{id:int}", (int id, StudyDataStore store) =>
            store.DeleteTask(id) ? Results.NoContent() : Results.NotFound());

        api.MapGet("/progress", (StudyDataStore store) =>
        {
            var tasks = store.GetTasks();
            var completed = tasks.Count(t => t.IsDone);
            return Results.Ok(new
            {
                totalTasks = tasks.Count,
                completedTasks = completed,
                pendingTasks = tasks.Count - completed,
                overdueTasks = tasks.Count(t => !t.IsDone && t.Deadline.Date < DateTime.Today),
                completionPercent = tasks.Count == 0 ? 0 : (int)Math.Round(100.0 * completed / tasks.Count)
            });
        });

        // Student directory and practice-exam results (total + score per subject).
        api.MapGet("/students", (StudyDataStore store) => Results.Ok(store.GetStudents()));

        api.MapPost("/students", (StudentInput input, StudyDataStore store) =>
        {
            if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 80)
                return Results.BadRequest(new { error = "Name must be 1-80 characters." });

            var student = store.AddStudent(input.Name);
            return Results.Created($"/api/students/{student.Id}", student);
        });

        api.MapGet("/students/{id:int}", (int id, StudyDataStore store) =>
        {
            var student = store.GetStudent(id);
            return student is null ? Results.NotFound() : Results.Ok(student);
        });

        api.MapGet("/exams", (int? studentId, StudyDataStore store) =>
            Results.Ok(store.GetExams(studentId)));

        api.MapPost("/exams", (ExamInput input, StudyDataStore store) =>
        {
            if (input.StudentId < 1)
                return Results.BadRequest(new { error = "Valid studentId is required." });
            if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200)
                return Results.BadRequest(new { error = "Title must be 1-200 characters." });
            if (input.TakenOn == default)
                return Results.BadRequest(new { error = "TakenOn is required." });

            var error = ValidateSubjects(input.Subjects);
            if (error is not null)
                return Results.BadRequest(new { error });

            var exam = store.AddExam(input.StudentId, input.Title, input.TakenOn, input.Subjects!);
            return exam is null
                ? Results.NotFound(new { error = "Student not found. Create the student first." })
                : Results.Created($"/api/exams/{exam.Id}", exam);
        });

        api.MapDelete("/exams/{id:int}", (int id, StudyDataStore store) =>
            store.DeleteExam(id) ? Results.NoContent() : Results.NotFound());

        api.MapGet("/students/{id:int}/progress", (int id, StudyDataStore store) =>
        {
            var student = store.GetStudent(id);
            if (student is null) return Results.NotFound();

            var exams = store.GetExams(id);
            var latest = exams.FirstOrDefault();
            var best = exams.OrderByDescending(e => e.Percentage).FirstOrDefault();

            return Results.Ok(new
            {
                student,
                examCount = exams.Count,
                latestExam = latest,
                bestExam = best,
                // Subject marks come from the latest exam; totals are computed by PracticeExam.
                latestSubjectScores = latest?.Subjects ?? new List<SubjectMark>()
            });
        });

        return app;
    }

    private static string? ValidateSubjects(List<SubjectMark>? subjects)
    {
        if (subjects is null || subjects.Count == 0)
            return "At least one subject is required.";
        if (subjects.Count > 20)
            return "Too many subjects (maximum 20).";

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var subject in subjects)
        {
            if (subject is null || string.IsNullOrWhiteSpace(subject.Subject) || subject.Subject.Length > 80)
                return "Every subject must have a name (1-80 characters).";
            if (!names.Add(subject.Subject.Trim()))
                return "The same subject cannot appear twice in one exam.";
            if (subject.MaxScore <= 0 || subject.MaxScore > 1000 || subject.Score < 0 || subject.Score > subject.MaxScore)
                return "Each score must be between 0 and maxScore, and maxScore must be between 1 and 1000.";
        }

        return null;
    }

    public sealed class TaskInput : TaskInputBase
    {
    }

    public sealed class TaskEditInput : TaskInputBase
    {
        public bool IsDone { get; set; }
    }

    public class TaskInputBase
    {
        public string Title { get; set; } = "";
        public string Subject { get; set; } = "Informatics";
        public DateTime Deadline { get; set; }
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
        public List<SubjectMark>? Subjects { get; set; } = new();
    }
}
