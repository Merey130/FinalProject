namespace StudyFlow.Models;

// One subject in a practice exam (e.g. Mathematics: 38 out of 50).
public sealed class SubjectMark
{
    public string Subject { get; set; } = "";
    public int Score { get; set; }
    public int MaxScore { get; set; }
}
