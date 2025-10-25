using Notes.Api.DTOs;
using Notes.Api.Services;

namespace Notes.Api.Endpoints;

public static class NotebookEndpoints
{
    public static void Map(WebApplication app) => app.MapGroup("/notebooks")
            .WithTags("Notebook")
            .MapNotebooksEndpoints()
            .AddEndpointFilter((context, next) =>
            {
                app.Logger.LogInformation("Endpoint Filter On Notebooks Group!");
                return next(context);
            })
            .RequireAuthorization();

    private static RouteGroupBuilder MapNotebooksEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", NotebookService.GetAllNotebooks)
            .Produces<List<NotebookDto>>(200);

        group.MapGet("/{notebookId:int}", NotebookService.GetNotebook)
            .Produces<NotebookDto>(200)
            .Produces(404);

        group.MapPost("/", NotebookService.CreateNotebook).Produces<NotebookDto>(201)
            .Produces(400);

        group.MapPut("/{notebookId:int}", NotebookService.UpdateNotebook)
            .Produces<NotebookDto>(204)
            .Produces(404);

        group.MapDelete("/{notebookId:int}", NotebookService.DeleteNotebook)
            .Produces<NotebookDto>(204)
            .Produces(404);

        return group;
    }
}