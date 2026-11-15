using StudyFlow.Models;

namespace StudyFlow.Services;

// The signed-in student's practice exams, backed by Supabase + RLS.
public sealed class MockExamState(StudyRepository repository, CurrentAccessToken token)
{
    private readonly List<MockExam> _results = new();

    public IReadOnlyList<MockExam> Results => _results;

    public async Task LoadAsync()
    {
        var rows = await repository.GetExams(await token.Get());
        _results.Clear();
        foreach (var row in rows)
        {
            _results.Add(new MockExam
            {
                Id = row.Id,
                Date = row.TakenOn,
                History = Score(row, "History"),
                Reading = Score(row, "Reading"),
                Literacy = Score(row, "Literacy"),
                Math = Score(row, "Math"),
                Informatics = Score(row, "Informatics")
            });
        }
    }

    public async Task AddResultAsync(MockExam exam)
    {
        var marks = new List<SubjectMark>
        {
            new() { Subject = "History", Score = exam.History, MaxScore = 20 },
            new() { Subject = "Reading", Score = exam.Reading, MaxScore = 10 },
            new() { Subject = "Literacy", Score = exam.Literacy, MaxScore = 10 },
            new() { Subject = "Math", Score = exam.Math, MaxScore = 50 },
            new() { Subject = "Informatics", Score = exam.Informatics, MaxScore = 50 }
        };
        var saved = await repository.AddExam(await token.Get(), "UNT Mock Exam", exam.Date, marks);
        exam.Id = saved.Id;
        _results.Add(exam);
    }

    public async Task DeleteResultAsync(int id)
    {
        await repository.DeleteExam(await token.Get(), id);
        _results.RemoveAll(x => x.Id == id);
    }

    private static int Score(PracticeExam exam, string subject) =>
        exam.Subjects.FirstOrDefault(s => s.Subject == subject)?.Score ?? 0;
}
