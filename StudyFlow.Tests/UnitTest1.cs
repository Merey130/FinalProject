
using StudyFlow.Models;
using Xunit;

namespace StudyFlow.Tests;

public class MockExamTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(20, 10, 10, 50, 50, 140)]
    [InlineData(15, 8, 7, 30, 29, 89)]
    public void Total_CalculatesCorrectly(
        int history,
        int reading,
        int literacy,
        int math,
        int informatics,
        int expected)
    {
        var exam = new MockExam
        {
            History = history,
            Reading = reading,
            Literacy = literacy,
            Math = math,
            Informatics = informatics
        };

        Assert.Equal(expected, exam.Total);
    }

    [Fact]
    public void Total_UpdatesWhenScoreChanges()
    {
        var exam = new MockExam
        {
            History = 10,
            Reading = 5,
            Literacy = 5,
            Math = 20,
            Informatics = 30
        };

        Assert.Equal(70, exam.Total);

        exam.Math = 40;

        Assert.Equal(90, exam.Total);
    }
}
