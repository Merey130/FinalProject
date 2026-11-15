using System.Globalization;
using System.Text.Json.Serialization;
using StudyFlow.Models;

namespace StudyFlow.Services;

public sealed class StudyRepository(SupabaseDatabase db)
{
    public async Task<List<StudyTask>> GetTasks(string token)
    {
        var rows = await db.Get<TaskRow>("sf_tasks?select=id,title,subject,deadline,is_done&order=deadline.asc", token);
        return rows.Select(Map).ToList();
    }
    public async Task<StudyTask> AddTask(string token, string title, string subject, DateTime deadline)
    {
        var row = await db.Insert<TaskRow>("sf_tasks?select=id,title,subject,deadline,is_done", token,
            new { title, subject, deadline = deadline.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
        return Map(row);
    }
    public async Task<StudyTask?> SetDone(string token, int id, bool done)
    {
        var rows = await db.Patch<TaskRow>($"sf_tasks?id=eq.{id}&select=id,title,subject,deadline,is_done",
            token, new { is_done = done });
        return rows.Select(Map).FirstOrDefault();
    }
    public Task DeleteTask(string token, int id) => db.Delete($"sf_tasks?id=eq.{id}", token);

    public async Task<List<PracticeExam>> GetExams(string token)
    {
        var exams = await db.Get<ExamRow>("sf_practice_exams?select=id,title,taken_on&order=taken_on.desc", token);
        var scores = await db.Get<ScoreRow>("sf_subject_scores?select=exam_id,subject,score,max_score", token);
        return exams.Select(e => new PracticeExam {
            Id=e.Id, Title=e.Title,
            TakenOn=DateTime.ParseExact(e.TakenOn, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Subjects=scores.Where(s=>s.ExamId==e.Id).Select(s=>new SubjectMark {
                Subject=s.Subject, Score=s.Score, MaxScore=s.MaxScore
            }).ToList()
        }).ToList();
    }
    public async Task<PracticeExam> AddExam(string token, string title, DateTime date, List<SubjectMark> subjects)
    {
        var exam = await db.Insert<ExamRow>("sf_practice_exams?select=id,title,taken_on", token,
            new { title, taken_on=date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
        try {
            foreach (var s in subjects)
                await db.Insert<ScoreRow>("sf_subject_scores?select=exam_id,subject,score,max_score", token,
                    new { exam_id=exam.Id, subject=s.Subject, score=s.Score, max_score=s.MaxScore });
        } catch {
            await db.Delete($"sf_practice_exams?id=eq.{exam.Id}", token);
            throw;
        }
        return new PracticeExam { Id=exam.Id, Title=exam.Title, TakenOn=date, Subjects=subjects };
    }
    public Task DeleteExam(string token, int id) => db.Delete($"sf_practice_exams?id=eq.{id}", token);

    private static StudyTask Map(TaskRow r) => new() {
        Id=r.Id, Title=r.Title, Subject=r.Subject,
        Deadline=DateTime.ParseExact(r.Deadline, "yyyy-MM-dd", CultureInfo.InvariantCulture),
        IsDone=r.IsDone
    };
    private sealed class TaskRow {
        [JsonPropertyName("id")] public int Id {get;set;}
        [JsonPropertyName("title")] public string Title {get;set;}="";
        [JsonPropertyName("subject")] public string Subject {get;set;}="";
        [JsonPropertyName("deadline")] public string Deadline {get;set;}="";
        [JsonPropertyName("is_done")] public bool IsDone {get;set;}
    }
    private sealed class ExamRow {
        [JsonPropertyName("id")] public int Id {get;set;}
        [JsonPropertyName("title")] public string Title {get;set;}="";
        [JsonPropertyName("taken_on")] public string TakenOn {get;set;}="";
    }
    private sealed class ScoreRow {
        [JsonPropertyName("exam_id")] public int ExamId {get;set;}
        [JsonPropertyName("subject")] public string Subject {get;set;}="";
        [JsonPropertyName("score")] public int Score {get;set;}
        [JsonPropertyName("max_score")] public int MaxScore {get;set;}
    }
}
