using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using StudyFlow.Models;

namespace StudyFlow.Services;

// Small local backend without additional NuGet dependencies.
// A single JSON file is suitable for a class-project demo, not a public multi-user deployment.
public sealed class StudyDataStore
{
    private readonly object _gate = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly StudyDatabase _db;

    public StudyDataStore(IWebHostEnvironment environment)
    {
        var folder = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "studyflow.json");

        if (!File.Exists(_filePath))
        {
            _db = new StudyDatabase();
            return;
        }

        try
        {
            _db = JsonSerializer.Deserialize<StudyDatabase>(
                File.ReadAllText(_filePath), _json)
                ?? throw new InvalidDataException("The StudyFlow data file is empty.");
        }
        catch (JsonException exception)
        {
            // Don't overwrite a damaged file: let the developer recover it.
            throw new InvalidDataException(
                $"Cannot read {_filePath}. Back up and repair this file before restarting.",
                exception);
        }

        _db.Tasks ??= new();
        _db.Students ??= new();
        _db.Exams ??= new();
    }

    public IReadOnlyList<StudyTask> GetTasks()
    {
        lock (_gate)
            return _db.Tasks.Select(CopyTask).OrderBy(t => t.Deadline).ToList();
    }

    public StudyTask AddTask(string title, string subject, DateTime deadline)
    {
        lock (_gate)
        {
            var task = new StudyTask
            {
                Id = NextId(_db.Tasks.Select(t => t.Id)),
                Title = title.Trim(),
                Subject = string.IsNullOrWhiteSpace(subject) ? "Other" : subject.Trim(),
                Deadline = deadline,
                IsDone = false
            };

            _db.Tasks.Add(task);
            Save();
            return CopyTask(task);
        }
    }

    public StudyTask? UpdateTask(int id, string title, string subject, DateTime deadline, bool isDone)
    {
        lock (_gate)
        {
            var task = _db.Tasks.Find(t => t.Id == id);
            if (task is null) return null;

            task.Title = title.Trim();
            task.Subject = string.IsNullOrWhiteSpace(subject) ? "Other" : subject.Trim();
            task.Deadline = deadline;
            task.IsDone = isDone;
            Save();
            return CopyTask(task);
        }
    }

    public bool ToggleTask(int id)
    {
        lock (_gate)
        {
            var task = _db.Tasks.Find(t => t.Id == id);
            if (task is null) return false;

            task.IsDone = !task.IsDone;
            Save();
            return true;
        }
    }

    public bool DeleteTask(int id)
    {
        lock (_gate)
        {
            var task = _db.Tasks.Find(t => t.Id == id);
            if (task is null) return false;

            _db.Tasks.Remove(task);
            Save();
            return true;
        }
    }

    public IReadOnlyList<Student> GetStudents()
    {
        lock (_gate)
            return _db.Students.Select(CopyStudent).OrderBy(s => s.Id).ToList();
    }

    public Student? GetStudent(int id)
    {
        lock (_gate)
        {
            var student = _db.Students.Find(s => s.Id == id);
            return student is null ? null : CopyStudent(student);
        }
    }

    public Student AddStudent(string name)
    {
        lock (_gate)
        {
            var student = new Student
            {
                Id = NextId(_db.Students.Select(s => s.Id)),
                Name = name.Trim()
            };
            _db.Students.Add(student);
            Save();
            return CopyStudent(student);
        }
    }

    public IReadOnlyList<PracticeExam> GetExams(int? studentId = null)
    {
        lock (_gate)
        {
            return _db.Exams
                .Where(e => studentId is null || e.StudentId == studentId)
                .OrderByDescending(e => e.TakenOn)
                .ThenByDescending(e => e.Id)
                .Select(CopyExam)
                .ToList();
        }
    }

    public PracticeExam? AddExam(int studentId, string title, DateTime takenOn, List<SubjectMark> subjects)
    {
        lock (_gate)
        {
            if (_db.Students.All(s => s.Id != studentId)) return null;

            var exam = new PracticeExam
            {
                Id = NextId(_db.Exams.Select(e => e.Id)),
                StudentId = studentId,
                Title = title.Trim(),
                TakenOn = takenOn,
                Subjects = subjects.Select(CopyMark).ToList()
            };
            _db.Exams.Add(exam);
            Save();
            return CopyExam(exam);
        }
    }

    public bool DeleteExam(int id)
    {
        lock (_gate)
        {
            var exam = _db.Exams.Find(e => e.Id == id);
            if (exam is null) return false;

            _db.Exams.Remove(exam);
            Save();
            return true;
        }
    }

    private void Save()
    {
        // Called while holding _gate. A temp file avoids half-written JSON.
        var temp = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(_db, _json));
            File.Move(temp, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static int NextId(IEnumerable<int> ids) => ids.DefaultIfEmpty(0).Max() + 1;

    private static StudyTask CopyTask(StudyTask t) => new()
    {
        Id = t.Id, Title = t.Title, Subject = t.Subject,
        Deadline = t.Deadline, IsDone = t.IsDone
    };

    private static Student CopyStudent(Student s) => new()
    {
        Id = s.Id, Name = s.Name
    };

    private static SubjectMark CopyMark(SubjectMark m) => new()
    {
        Subject = m.Subject, Score = m.Score, MaxScore = m.MaxScore
    };

    private static PracticeExam CopyExam(PracticeExam e) => new()
    {
        Id = e.Id, StudentId = e.StudentId, Title = e.Title,
        TakenOn = e.TakenOn, Subjects = e.Subjects.Select(CopyMark).ToList()
    };
}
