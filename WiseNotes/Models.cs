using Microsoft.AspNetCore.Identity;

namespace WiseNotes;

public class User : IdentityUser
{
    public List<Notebook> Notebooks { get; set; } = [];
}

public class Notebook
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public List<Note> Notes { get; set; } = [];

    public string? UserId { get; set; }
    public User? User { get; set; }
}

public class Note
{
    public int Id { get; set; }
    public string? Content { get; set; }

    public int NotebookId { get; set; }
    public Notebook? Notebook { get; set; }
}
