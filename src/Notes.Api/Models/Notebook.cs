namespace Notes.Api.Models;

public class Notebook
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public List<Note> Notes { get; set; } = [];

    public string? UserId { get; set; }
    public User? User { get; set; }
}
