namespace WiseNotes.Tests;

public class NotebookServiceTests
{
    [Fact]
    public void Add_ShouldReturnSum_WhenTwoIntegersProvided()
    {
        // Arrange
        var service = new NotebookService();

        // Act
        var result = service.Add(2, 3);

        // Assert
        Assert.Equal(5, result);
    }
}
