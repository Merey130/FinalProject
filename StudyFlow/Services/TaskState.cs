using StudyFlow.Models;

namespace StudyFlow.Services;

// Compatibility adapter for the current Home.razor and MyTasks.razor.
// They call synchronous methods, so this adapter bridges them to the DB repository.
// For a future production app, migrate the Razor event handlers to async instead.
public sealed class TaskState
{
    private readonly StudyRepository _repo;
    private List<StudyTask>? _cachedTasks;

    public TaskState(StudyRepository repo) => _repo = repo;

    // On a new browser session this loads tasks from Supabase, not RAM.
    public IReadOnlyList<StudyTask> Tasks => EnsureLoaded();

    public void AddTask(string title, string subject, DateTime deadline)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        var tasks = EnsureLoaded();
        var added = _repo.CreateTaskAsync(title, subject, deadline)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        tasks.Add(added);
    }

    public void ToggleTask(int id)
    {
        var tasks = EnsureLoaded();
        var updated = _repo.ToggleTaskAsync(id)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        if (updated is null) return;
        var current = tasks.FirstOrDefault(x => x.Id == id);
        if (current is not null) current.IsDone = updated.IsDone;
    }

    public void DeleteTask(int id)
    {
        var tasks = EnsureLoaded();
        var deleted = _repo.DeleteTaskAsync(id)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        if (deleted) tasks.RemoveAll(x => x.Id == id);
    }

    private List<StudyTask> EnsureLoaded() => _cachedTasks ??=
        _repo.GetTasksAsync().ConfigureAwait(false).GetAwaiter().GetResult();
}
