using WiseNotes.DTOs;
using WiseNotes.Services;

namespace WiseNotes.Endpoints;

public static class NoteEndpoints
{
    public static void Map(WebApplication app) => app.MapGroup("/notebooks/{notebookId:int}/notes")        
        .WithTags("Notebooks: Notes")
        .MapNotesEndpoints()
        .AddEndpointFilter((context, next) =>
        {
            app.Logger.LogInformation("Endpoint Filter On Notes Group!");
            return next(context);
        })
        .RequireAuthorization();

    private static RouteGroupBuilder MapNotesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", NoteService.GetAllNotes)
            .Produces<List<NoteDto>>(200);

        group.MapGet("/{noteId:int}", NoteService.GetNote)
            .Produces<NoteDto>(200)
            .Produces(404);

        group.MapPost("/", NoteService.CreateNote)
            .Produces<NoteDto>(201)
            .Produces(400);

        group.MapPut("/{noteId:int}", NoteService.UpdateNote)
            .Produces<NoteDto>(204)
            .Produces(404);

        group.MapDelete("/{noteId:int}", NoteService.DeleteNote)
            .Produces<NoteDto>(204)
            .Produces(404);

        return group;
    }
}
