
using StudyFlow.Models;

namespace StudyFlow.Services;

public class MockExamState
{
    private readonly List<MockExam> _results = new();

    private int _nextId = 1;

    public IReadOnlyList<MockExam> Results => _results;

    public void AddResult(MockExam exam)
    {
        exam.Id = _nextId++;

        _results.Add(exam);
    }

    public void DeleteResult(int id)
    {
        var exam = _results.Find(r => r.Id == id);

        if (exam != null)
        {
            _results.Remove(exam);
        }
    }
}
