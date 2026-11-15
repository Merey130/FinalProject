using Npgsql;
using NpgsqlTypes;
using StudyFlow.Models;

namespace StudyFlow.Services;

// Data layer. Parameterized commands prevent SQL injection.
// All data is stored in Supabase PostgreSQL, not in a local JSON file.
public sealed class StudyRepository
{
    private readonly NpgsqlDataSource _database;

    public StudyRepository(NpgsqlDataSource database) => _database = database;

    public async Task<bool> CanConnectAsync(CancellationToken ct = default)
    {
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand("SELECT 1", conn);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false)) == 1;
    }

    private static StudyTask ReadTask(NpgsqlDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Title = r.GetString(1),
        Subject = r.GetString(2),
        Deadline = r.GetFieldValue<DateOnly>(3).ToDateTime(TimeOnly.MinValue),
        IsDone = r.GetBoolean(4)
    };

    public async Task<List<StudyTask>> GetTasksAsync(CancellationToken ct = default)
    {
        var result = new List<StudyTask>();
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, title, subject, deadline, is_done FROM public.study_tasks ORDER BY deadline, id", conn);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false)) result.Add(ReadTask(r));
        return result;
    }

    public async Task<StudyTask> CreateTaskAsync(
        string title, string subject, DateTime deadline, CancellationToken ct = default)
    {
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO public.study_tasks (title, subject, deadline)
            VALUES (@title, @subject, @deadline)
            RETURNING id, title, subject, deadline, is_done", conn);
        cmd.Parameters.AddWithValue("title", NpgsqlDbType.Text, title.Trim());
        cmd.Parameters.AddWithValue("subject", NpgsqlDbType.Text, subject.Trim());
        cmd.Parameters.AddWithValue("deadline", NpgsqlDbType.Date, DateOnly.FromDateTime(deadline));
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await r.ReadAsync(ct).ConfigureAwait(false))
            throw new InvalidOperationException("Inserting task did not return a row.");
        return ReadTask(r);
    }

    public async Task<StudyTask?> UpdateTaskAsync(
        int id, string title, string subject, DateTime deadline, bool isDone,
        CancellationToken ct = default)
    {
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE public.study_tasks
            SET title = @title, subject = @subject, deadline = @deadline, is_done = @is_done
            WHERE id = @id
            RETURNING id, title, subject, deadline, is_done", conn);
        cmd.Parameters.AddWithValue("id", NpgsqlDbType.Integer, id);
        cmd.Parameters.AddWithValue("title", NpgsqlDbType.Text, title.Trim());
        cmd.Parameters.AddWithValue("subject", NpgsqlDbType.Text, subject.Trim());
        cmd.Parameters.AddWithValue("deadline", NpgsqlDbType.Date, DateOnly.FromDateTime(deadline));
        cmd.Parameters.AddWithValue("is_done", NpgsqlDbType.Boolean, isDone);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await r.ReadAsync(ct).ConfigureAwait(false) ? ReadTask(r) : null;
    }

    public async Task<StudyTask?> ToggleTaskAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(@"
            UPDATE public.study_tasks SET is_done = NOT is_done WHERE id = @id
            RETURNING id, title, subject, deadline, is_done", conn);
        cmd.Parameters.AddWithValue("id", NpgsqlDbType.Integer, id);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await r.ReadAsync(ct).ConfigureAwait(false) ? ReadTask(r) : null;
    }

    public async Task<bool> DeleteTaskAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM public.study_tasks WHERE id = @id RETURNING id", conn);
        cmd.Parameters.AddWithValue("id", NpgsqlDbType.Integer, id);
        return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) is not null;
    }

    private static Student ReadStudent(NpgsqlDataReader r) =>
        new() { Id = r.GetInt32(0), Name = r.GetString(1) };

    public async Task<List<Student>> GetStudentsAsync(CancellationToken ct = default)
    {
        var list = new List<Student>();
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name FROM public.study_students ORDER BY id", conn);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false)) list.Add(ReadStudent(r));
        return list;
    }

    public async Task<Student?> GetStudentAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name FROM public.study_students WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", NpgsqlDbType.Integer, id);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await r.ReadAsync(ct).ConfigureAwait(false) ? ReadStudent(r) : null;
    }

    public async Task<Student> CreateStudentAsync(string name, CancellationToken ct = default)
    {
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO public.study_students (name) VALUES (@name) RETURNING id, name", conn);
        cmd.Parameters.AddWithValue("name", NpgsqlDbType.Text, name.Trim());
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await r.ReadAsync(ct).ConfigureAwait(false))
            throw new InvalidOperationException("Inserting student did not return a row.");
        return ReadStudent(r);
    }

    public async Task<List<PracticeExam>> GetExamsAsync(
        int? studentId = null, CancellationToken ct = default)
    {
        var list = new List<PracticeExam>();
        var byId = new Dictionary<int, PracticeExam>();
        var sql = @"
            SELECT e.id, e.student_id, e.title, e.taken_on,
                   s.subject, s.score, s.max_score
            FROM public.study_practice_exams e
            LEFT JOIN public.study_subject_scores s ON s.exam_id = e.id
            " + (studentId.HasValue ? "WHERE e.student_id = @student_id " : "") + @"
            ORDER BY e.taken_on DESC, e.id DESC, s.subject";
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        if (studentId.HasValue)
            cmd.Parameters.AddWithValue("student_id", NpgsqlDbType.Integer, studentId.Value);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            var id = r.GetInt32(0);
            if (!byId.TryGetValue(id, out var exam))
            {
                exam = new PracticeExam
                {
                    Id = id,
                    StudentId = r.GetInt32(1),
                    Title = r.GetString(2),
                    TakenOn = r.GetFieldValue<DateOnly>(3).ToDateTime(TimeOnly.MinValue)
                };
                byId.Add(id, exam);
                list.Add(exam);
            }
            if (!r.IsDBNull(4))
            {
                exam.Subjects.Add(new SubjectMark
                {
                    Subject = r.GetString(4),
                    Score = r.GetInt32(5),
                    MaxScore = r.GetInt32(6)
                });
            }
        }
        return list;
    }

    public async Task<PracticeExam?> CreateExamAsync(
        int studentId, string title, DateTime takenOn, List<SubjectMark> subjects,
        CancellationToken ct = default)
    {
        if (await GetStudentAsync(studentId, ct).ConfigureAwait(false) is null) return null;
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO public.study_practice_exams (student_id, title, taken_on)
            VALUES (@student_id, @title, @taken_on) RETURNING id", conn, tx);
        cmd.Parameters.AddWithValue("student_id", NpgsqlDbType.Integer, studentId);
        cmd.Parameters.AddWithValue("title", NpgsqlDbType.Text, title.Trim());
        cmd.Parameters.AddWithValue("taken_on", NpgsqlDbType.Date, DateOnly.FromDateTime(takenOn));
        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false));

        foreach (var s in subjects)
        {
            await using var scoreCmd = new NpgsqlCommand(@"
                INSERT INTO public.study_subject_scores (exam_id, subject, score, max_score)
                VALUES (@exam_id, @subject, @score, @max_score)", conn, tx);
            scoreCmd.Parameters.AddWithValue("exam_id", NpgsqlDbType.Integer, id);
            scoreCmd.Parameters.AddWithValue("subject", NpgsqlDbType.Text, s.Subject.Trim());
            scoreCmd.Parameters.AddWithValue("score", NpgsqlDbType.Integer, s.Score);
            scoreCmd.Parameters.AddWithValue("max_score", NpgsqlDbType.Integer, s.MaxScore);
            await scoreCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        await tx.CommitAsync(ct).ConfigureAwait(false);

        return new PracticeExam
        {
            Id = id,
            StudentId = studentId,
            Title = title.Trim(),
            TakenOn = takenOn.Date,
            Subjects = subjects.Select(s => new SubjectMark
            {
                Subject = s.Subject.Trim(), Score = s.Score, MaxScore = s.MaxScore
            }).ToList()
        };
    }

    public async Task<bool> DeleteExamAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await _database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM public.study_practice_exams WHERE id = @id RETURNING id", conn);
        cmd.Parameters.AddWithValue("id", NpgsqlDbType.Integer, id);
        return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) is not null;
    }
}
