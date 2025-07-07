namespace WiseNotes;

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

public record NoteDto(int Id, string Content);
public record CreateNotebookDto(string Title);