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
/// Provides notebook-related operations such as retrieving, creating, updating, and deleting notebooks for authenticated users.
/// </summary>
public static class NotebookService
{
    /// <summary>
    /// Retrieves all notebooks belonging to the authenticated user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="user">The user manager for user operations.</param>
    /// <param name="claims">The claims principal representing the current user.</param>
    /// <returns>A list of notebooks if authorized, otherwise an unauthorized result.</returns>
    public static async Task<Results<Ok<List<NotebookDto>>, UnauthorizedHttpResult>> GetAllNotebooks(AppDbContext db,
        UserManager<User> user, ClaimsPrincipal claims)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

        var notebooks = await db.Notebooks
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .Select(n => new NotebookDto
            {
                Id = n.Id,
                Title = n.Title
            }).ToListAsync();

        return TypedResults.Ok(notebooks);
    }

    /// <summary>
    /// Retrieves a specific notebook by its ID for the authenticated user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="user">The user manager for user operations.</param>
    /// <param name="claims">The claims principal representing the current user.</param>
    /// <param name="notebookId">The ID of the notebook to retrieve.</param>
    /// <returns>The notebook if found and authorized, otherwise not found or unauthorized result.</returns>
    public static async Task<Results<Ok<NotebookDto>, NotFound, UnauthorizedHttpResult>> GetNotebook(AppDbContext db,
        UserManager<User> user, ClaimsPrincipal claims, int notebookId)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

        var notebook = await db.Notebooks
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.Id == notebookId)
            .Select(n => new NotebookDto
            {
                Id = n.Id,
                Title = n.Title,
                Notes = n.Notes.Count
            }).FirstOrDefaultAsync();

        return notebook != null ? TypedResults.Ok(notebook) : TypedResults.NotFound();
    }

    /// <summary>
    /// Creates a new notebook for the authenticated user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="user">The user manager for user operations.</param>
    /// <param name="claims">The claims principal representing the current user.</param>
    /// <param name="request">The notebook creation request data.</param>
    /// <returns>The created notebook if successful, otherwise a bad request or unauthorized result.</returns>
    public static async Task<Results<Created<NotebookDto>, BadRequest<ErrorResponse>, UnauthorizedHttpResult>> CreateNotebook(AppDbContext db,
        UserManager<User> user, ClaimsPrincipal claims, CreateNotebookDto request)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Title), "Title is required"));

        if (request.Title.Length > Constants.TitleLength)
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Title), $"Title must be less then {Constants.TitleLength} characters."));

        var notebook = new Notebook
        {
            Title = request.Title,
            UserId = userId,
        };

        db.Notebooks.Add(notebook);
        await db.SaveChangesAsync();

        var notebookDto = new NotebookDto
        {
            Id = notebook.Id,
            Title = notebook.Title,
            Notes = notebook.Notes.Count
        };

        return TypedResults.Created($"/notebooks/{notebookDto.Id}", notebookDto);
    }

    /// <summary>
    /// Updates the title of an existing notebook for the authenticated user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="user">The user manager for user operations.</param>
    /// <param name="claims">The claims principal representing the current user.</param>
    /// <param name="request">The notebook update request data.</param>
    /// <param name="notebookId">The ID of the notebook to update.</param>
    /// <returns>No content if successful, otherwise not found, bad request, or unauthorized result.</returns>
    public static async Task<Results<NotFound, NoContent, BadRequest<ErrorResponse>, UnauthorizedHttpResult>> UpdateNotebook(AppDbContext db,
        UserManager<User> user, ClaimsPrincipal claims, CreateNotebookDto request, int notebookId)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

        var notebook = await db.Notebooks.FirstOrDefaultAsync(nb => nb.Id == notebookId && nb.UserId == userId);

        if (notebook != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return TypedResults.BadRequest(new ErrorResponse(nameof(request.Title), "Title is required"));

            if (request.Title.Length > Constants.TitleLength)
                return TypedResults.BadRequest(new ErrorResponse(nameof(request.Title), $"Title must be less then {Constants.TitleLength}"));

            notebook.Title = request.Title;
            await db.SaveChangesAsync();
        }
        else
            return TypedResults.NotFound();

        return TypedResults.NoContent();
    }

    /// <summary>
    /// Deletes a notebook for the authenticated user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="user">The user manager for user operations.</param>
    /// <param name="claims">The claims principal representing the current user.</param>
    /// <param name="notebookId">The ID of the notebook to delete.</param>
    /// <returns>No content if successful, otherwise not found or unauthorized result.</returns>
    public static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> DeleteNotebook(AppDbContext db,
        UserManager<User> user, ClaimsPrincipal claims, int notebookId)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

        var notebook = await db.Notebooks.FirstOrDefaultAsync(nb => nb.Id == notebookId && nb.UserId == userId);

        if (notebook != null)
        {
            db.Notebooks.Remove(notebook);
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        }

        return TypedResults.NotFound();
    }
}