using StudyFlow.Models;

namespace StudyFlow.Services;

// Existing .razor files can continue to call TaskState exactly as before.
// Unlike the previous in-memory version, changes are now saved by StudyDataStore.
public sealed class TaskState
{
    private readonly StudyDataStore _store;

    public TaskState(StudyDataStore store)
    {
        _store = store;
    }

    public IReadOnlyList<StudyTask> Tasks => _store.GetTasks();

    public void AddTask(string title, string subject, DateTime deadline)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        _store.AddTask(title, subject, deadline);
    }

    public void ToggleTask(int id) => _store.ToggleTask(id);

    public void DeleteTask(int id) => _store.DeleteTask(id);
}
