namespace StudyFlow.Models;

// Stored in StudyFlow/App_Data/studyflow.json (not published through wwwroot).
public sealed class StudyDatabase
{
    public List<StudyTask> Tasks { get; set; } = new();
    public List<Student> Students { get; set; } = new();
    public List<PracticeExam> Exams { get; set; } = new();
}
