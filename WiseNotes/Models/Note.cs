namespace WiseNotes.Models;

public class Note
{
    public int Id { get; set; }
    public string? Content { get; set; }

    public int NotebookId { get; set; }
    public Notebook? Notebook { get; set; }
}
