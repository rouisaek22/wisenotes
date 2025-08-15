using WiseNotes.Models;

namespace WiseNotes.DTOs;

public record NotebookDto(int Id, string Title, int Count)
{
    public NotebookDto(Notebook notebook)
        : this(notebook.Id, notebook.Title, notebook.Notes.Count)
    {
        this.Id = notebook.Id;
        this.Title = notebook.Title;
        this.Count = notebook.Notes.Count;
    }
}
