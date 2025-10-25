using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Notes.Api.Database;
using Notes.Api.DTOs;
using Notes.Api.Models;
using Notes.Api.Validations;

namespace Notes.Api.Services;

/// <summary>
/// Provides methods for managing notes within the application.
/// </summary>
public static class NoteService
{
    /// <summary>
    /// Retrieves all notes for a specific notebook belonging to the authenticated user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="user">The user manager for user operations.</param>
    /// <param name="claims">The claims principal representing the current user.</param>
    /// <param name="notebookId">The ID of the notebook to retrieve notes from.</param>
    /// <returns>A list of notes if authorized, otherwise an unauthorized result.</returns>
    public static async Task<Results<Ok<List<NoteDto>>, UnauthorizedHttpResult>> GetAllNotes(AppDbContext db,
        UserManager<User> user, ClaimsPrincipal claims, int notebookId)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

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

    /// <summary>
    /// Retrieves a specific note by its ID for the authenticated user and notebook.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="user">The user manager for user operations.</param>
    /// <param name="claims">The claims principal representing the current user.</param>
    /// <param name="notebookId">The ID of the notebook containing the note.</param>
    /// <param name="noteId">The ID of the note to retrieve.</param>
    /// <returns>The note if found and authorized, otherwise not found or unauthorized result.</returns>
    public static async Task<Results<Ok<NoteDto>, NotFound, UnauthorizedHttpResult>> GetNote(AppDbContext db,
        UserManager<User> user, ClaimsPrincipal claims, int notebookId, int noteId)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

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

    /// <summary>
    /// Creates a new note in the specified notebook for the authenticated user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="user">The user manager for user operations.</param>
    /// <param name="claims">The claims principal representing the current user.</param>
    /// <param name="request">The note creation request data.</param>
    /// <param name="notebookId">The ID of the notebook to add the note to.</param>
    /// <returns>The created note if successful, otherwise a bad request, forbidden, or unauthorized result.</returns>
    public static async Task<Results<Created<NoteDto>, BadRequest<ErrorResponse>, ForbidHttpResult, UnauthorizedHttpResult>> CreateNote(AppDbContext db,
        UserManager<User> user, ClaimsPrincipal claims, CreateNoteDto request, int notebookId)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Content), "Content is required"));

        if (request.Content.Length > Constants.ContentLength)
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Content), $"Content must be less then {Constants.ContentLength} characters."));

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

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException dex)
        {
            throw;
        }

        var noteDto = new NoteDto
        {
            Id = note.Id,
            Content = note.Content
        };

        return TypedResults.Created($"/notebooks/{notebookId}/notes/{noteDto.Id}", noteDto);
    }

    /// <summary>
    /// Updates the content of an existing note for the authenticated user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="user">The user manager for user operations.</param>
    /// <param name="claims">The claims principal representing the current user.</param>
    /// <param name="notebookId">The ID of the notebook containing the note.</param>
    /// <param name="noteId">The ID of the note to update.</param>
    /// <param name="request">The note update request data.</param>
    /// <returns>The updated note if successful, otherwise not found, bad request, or unauthorized result.</returns>
    public static async Task<Results<Ok<NoteDto>, NotFound, BadRequest<ErrorResponse>, UnauthorizedHttpResult>> UpdateNote(AppDbContext db,
        UserManager<User> user, ClaimsPrincipal claims, int notebookId, int noteId, CreateNoteDto request)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Content), "Content is required"));

        if (request.Content.Length > Constants.ContentLength)
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Content), $"Content must be less then {Constants.ContentLength}"));

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

    /// <summary>
    /// Deletes a note from the specified notebook for the authenticated user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="user">The user manager for user operations.</param>
    /// <param name="claims">The claims principal representing the current user.</param>
    /// <param name="notebookId">The ID of the notebook containing the note.</param>
    /// <param name="noteId">The ID of the note to delete.</param>
    /// <returns>No content if successful, otherwise not found or unauthorized result.</returns>
    public static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> DeleteNote(AppDbContext db,
        UserManager<User> user, ClaimsPrincipal claims, int notebookId, int noteId)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

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
