
namespace StudyFlow.Models;

public class MockExam
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public int History { get; set; }

    public int Reading { get; set; }

    public int Literacy { get; set; }

    public int Math { get; set; }

    public int Informatics { get; set; }

    public int Total =>
        History + Reading + Literacy
        + Math + Informatics;
}
