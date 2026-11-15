using StudyFlow.Models;

namespace StudyFlow.Services;

public sealed class TaskState(StudyRepository repo, CurrentAccessToken current)
{
    private readonly List<StudyTask> _tasks = [];
    public IReadOnlyList<StudyTask> Tasks => _tasks;

    public async Task LoadAsync()
    {
        var rows = await repo.GetTasks(await current.Get());
        _tasks.Clear(); _tasks.AddRange(rows);
    }
    public async Task AddTaskAsync(string title, string subject, DateTime deadline)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        var row = await repo.AddTask(await current.Get(), title.Trim(), subject, deadline);
        _tasks.Add(row);
    }
    public async Task ToggleTaskAsync(int id)
    {
        var task = _tasks.FirstOrDefault(t=>t.Id==id);
        if (task is null) return;
        var updated = await repo.SetDone(await current.Get(), id, !task.IsDone);
        if (updated is not null) task.IsDone=updated.IsDone;
    }
    public async Task DeleteTaskAsync(int id)
    {
        await repo.DeleteTask(await current.Get(), id);
        _tasks.RemoveAll(t=>t.Id==id);
    }
}
