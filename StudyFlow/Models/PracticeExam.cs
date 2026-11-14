namespace StudyFlow.Models;

public sealed class PracticeExam
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string Title { get; set; } = "";
    public DateTime TakenOn { get; set; }
    public List<SubjectMark> Subjects { get; set; } = new();

    // Computed automatically; no need to send or edit the total manually.
    public int TotalScore => Subjects.Sum(s => s.Score);
    public int MaxTotalScore => Subjects.Sum(s => s.MaxScore);
    public double Percentage => MaxTotalScore == 0
        ? 0
        : Math.Round(100.0 * TotalScore / MaxTotalScore, 1);
}
