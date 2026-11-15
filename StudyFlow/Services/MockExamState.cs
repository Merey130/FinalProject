
using StudyFlow.Models;

namespace StudyFlow.Services;

public class MockExamState
{
    private readonly StudyDataStore _store;
    private readonly int _studentId;

    public MockExamState(StudyDataStore store)
    {
        _store = store;

        // Temporary student for local demo only
        const string demoName = "StudyFlow Demo Student";

        var student = _store.GetStudents()
            .FirstOrDefault(s => s.Name == demoName);

        student ??= _store.AddStudent(demoName);

        _studentId = student.Id;
    }

    public IReadOnlyList<MockExam> Results =>
        _store.GetExams(_studentId)
            .Select(exam => new MockExam
            {
                Id = exam.Id,
                Date = exam.TakenOn,
                History = GetScore(exam, "History"),
                Reading = GetScore(exam, "Reading"),
                Literacy = GetScore(exam, "Literacy"),
                Math = GetScore(exam, "Math"),
                Informatics = GetScore(exam, "Informatics")
            })
            .ToList();

    public void AddResult(MockExam exam)
    {
        var subjects = new List<SubjectMark>
        {
            new()
            {
                Subject = "History",
                Score = exam.History,
                MaxScore = 20
            },
            new()
            {
                Subject = "Reading",
                Score = exam.Reading,
                MaxScore = 10
            },
            new()
            {
                Subject = "Literacy",
                Score = exam.Literacy,
                MaxScore = 10
            },
            new()
            {
                Subject = "Math",
                Score = exam.Math,
                MaxScore = 50
            },
            new()
            {
                Subject = "Informatics",
                Score = exam.Informatics,
                MaxScore = 50
            }
        };

        var savedExam = _store.AddExam(
            _studentId,
            "UNT Mock Exam",
            exam.Date,
            subjects
        );

        if (savedExam != null)
        {
            exam.Id = savedExam.Id;
        }
    }

    public void DeleteResult(int id)
    {
        _store.DeleteExam(id);
    }

    private static int GetScore(
        PracticeExam exam,
        string subject)
    {
        return exam.Subjects
            .FirstOrDefault(s => s.Subject == subject)
            ?.Score ?? 0;
    }
}
