using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WiseNotes.Database;
using WiseNotes.DTOs;
using WiseNotes.Models;

namespace WiseNotes.Services;

public static class NotebookService
{
    public static async Task<Ok<List<NotebookDto>>> GetAllNotebooks(AppDbContext db, UserManager<User> user, ClaimsPrincipal claims)
    {
        var userId = user.GetUserId(claims);

        var notebooks = await db.Notebooks
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(n => new NotebookDto
            {
                Id = n.Id,
                Title = n.Title
            }).ToListAsync();

        return TypedResults.Ok(notebooks);
    }

    public static async Task<Results<Ok<NotebookDto>, NotFound>> GetNotebook(AppDbContext db, UserManager<User> user, ClaimsPrincipal claims, int notebookId)
    {
        var userId = user.GetUserId(claims);

        var notebook = await db.Notebooks
            .AsNoTracking()
            .Where(u => u.UserId == userId && u.Id == notebookId)
            .Select(n => new NotebookDto
            {
                Id = n.Id,
                Title = n.Title,
                Notes = n.Notes.Count
            }).FirstOrDefaultAsync();

        return notebook != null ? TypedResults.Ok(notebook) : TypedResults.NotFound();
    }

    public static async Task<Results<Created<NotebookDto>, BadRequest<ErrorResponse>>> CreateNotebook(AppDbContext db, UserManager<User> user, ClaimsPrincipal claims, CreateNotebookDto request)
    {
        var userId = user.GetUserId(claims);

        if (string.IsNullOrWhiteSpace(request.Title))
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Title), "Title is required"));

        const int titleLength = 50;

        if (request.Title.Length > titleLength)
            return TypedResults.BadRequest(new ErrorResponse(nameof(request.Title), $"Title must be less then {titleLength}"));

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

    public static async Task<Results<NotFound, NoContent, BadRequest<ErrorResponse>>> UpdateNotebook(AppDbContext db, UserManager<User> user, ClaimsPrincipal claims, CreateNotebookDto request, int notebookId)
    {
        var userId = user.GetUserId(claims);

        var notebook = await db.Notebooks.FirstOrDefaultAsync(nb => nb.Id == notebookId && nb.UserId == userId);

        if (notebook != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return TypedResults.BadRequest(new ErrorResponse(nameof(request.Title), "Title is required"));

            const int titleLength = 50;

            if (request.Title.Length > titleLength)
                return TypedResults.BadRequest(new ErrorResponse(nameof(request.Title), $"Title must be less then {titleLength}"));

            notebook.Title = request.Title;
            await db.SaveChangesAsync();
        }
        else
            return TypedResults.NotFound();

        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteNotebook(AppDbContext db, UserManager<User> user, ClaimsPrincipal claims, int notebookId)
    {
        var userId = user.GetUserId(claims);

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