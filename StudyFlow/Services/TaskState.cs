
using StudyFlow.Models;

namespace StudyFlow.Services;

public class TaskState
{
    private readonly List<StudyTask> _tasks = new();

    private int _nextId = 1;

    public IReadOnlyList<StudyTask> Tasks => _tasks;

    public void AddTask(string title, string subject, DateTime deadline)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        _tasks.Add(new StudyTask
        {
            Id = _nextId++,
            Title = title.Trim(),
            Subject = subject,
            Deadline = deadline,
            IsDone = false
        });
    }

    public void ToggleTask(int id)
    {
        var task = _tasks.Find(t => t.Id == id);

        if (task != null)
        {
            task.IsDone = !task.IsDone;
        }
    }

    public void DeleteTask(int id)
    {
        var task = _tasks.Find(t => t.Id == id);

        if (task != null)
        {
            _tasks.Remove(task);
        }
    }
}
