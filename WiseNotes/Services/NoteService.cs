using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WiseNotes.Database;
using WiseNotes.DTOs;
using WiseNotes.Models;

namespace WiseNotes.Services;

public static class NoteService
{
    public static async Task<Ok<List<NoteDto>>> GetAllNotes(
        AppDbContext db,
        UserManager<User> user,
        ClaimsPrincipal claims,
        int notebookId)
    {
        var userId = user.GetUserId(claims);

        var notes = await db.Notes
            .AsNoTracking()
            .Where(n => n.Notebook.UserId == userId && n.NotebookId == notebookId)
            .Select(note => new NoteDto()
            {
                Id = note.Id,
                Content = note.Content
            }).ToListAsync();

        return TypedResults.Ok(notes);
    }

    public static async Task<Results<Ok<NoteDto>, NotFound>> GetNote(
        AppDbContext db,
        UserManager<User> user,
        ClaimsPrincipal claims,
        int notebookId,
        int noteId)
    {
        var userId = user.GetUserId(claims);

        var note = await db.Notes
                   .AsNoTracking()
                   .Where(n => n.Notebook.UserId == userId && n.NotebookId == notebookId && n.Id == noteId)
                   .Select(n => new NoteDto
                   {
                       Id = n.Id,
                       Content = n.Content
                   })
                   .FirstOrDefaultAsync();

        return note != null ? TypedResults.Ok(note) : TypedResults.NotFound();
    }

    public static async Task<Results<Created<NoteDto>, BadRequest<ErrorResponse>, ForbidHttpResult>> CreateNote(
        AppDbContext db,
        UserManager<User> userManager,
        ClaimsPrincipal claims,
        CreateNoteDto request,
        int notebookId)
    {
        var userId = userManager.GetUserId(claims);

        if (string.IsNullOrWhiteSpace(request.Content))
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Content), "Content is required"));

        const int contentLength = 500;

        if (request.Content.Length > contentLength)
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Content), $"Content must be less then {contentLength}"));

        // Check that the notebook exists and belongs to this user
        var notebook = await db.Notebooks
            .Where(n => n.Id == notebookId && n.UserId == userId)
            .FirstOrDefaultAsync();

        if (notebook == null)
        {
            return TypedResults.Forbid();
        }

        var note = new Note
        {
            Content = request.Content,
            NotebookId = notebookId
        };

        db.Notes.Add(note);
        await db.SaveChangesAsync();

        var noteDto = new NoteDto
        {
            Id = note.Id,
            Content = note.Content
        };

        return TypedResults.Created($"/notebooks/{notebookId}/notes/{noteDto.Id}", noteDto);
    }

    public static async Task<Results<Ok<NoteDto>, NotFound, BadRequest<ErrorResponse>>> UpdateNote(
        AppDbContext db,
        UserManager<User> user,
        ClaimsPrincipal claims,
        int notebookId,
        int noteId,
        CreateNoteDto request)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrWhiteSpace(request.Content))
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Content), "Content is required"));

        const int contentLength = 500;

        if (request.Content.Length > contentLength)
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Content), $"Content must be less then {contentLength}"));

        var note = await db.Notes
            .Include(n => n.Notebook)
            .Where(n => n.NotebookId == notebookId && n.Id == noteId && n.Notebook.UserId == userId)
            .FirstOrDefaultAsync();

        if (note is null)
            return TypedResults.NotFound();

        note.Content = request.Content;
        await db.SaveChangesAsync();

        var dto = new NoteDto
        {
            Id = note.Id,
            Content = note.Content
        };

        return TypedResults.Ok(dto);
    }

    public static async Task<Results<NoContent, NotFound>> DeleteNote(
    AppDbContext db,
    UserManager<User> user,
    ClaimsPrincipal claims,
    int notebookId,
    int noteId)
    {
        var userId = user.GetUserId(claims);

        var note = await db.Notes
            .Include(n => n.Notebook)
            .Where(n => n.NotebookId == notebookId && n.Id == noteId && n.Notebook.UserId == userId)
            .FirstOrDefaultAsync();

        if (note is null)
            return TypedResults.NotFound();

        db.Notes.Remove(note);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }


}
