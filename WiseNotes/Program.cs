using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WiseNotes;

// var builder = WebApplication.CreateBuilder(args);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    EnvironmentName = Environments.Development,
    WebRootPath = "wwwroot"
});

Console.WriteLine($"🚀 Running in: {builder.Environment.EnvironmentName}");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        var certPath = Path.Combine(builder.Environment.ContentRootPath, "localhost.pem");
        var keyPath = Path.Combine(builder.Environment.ContentRootPath, "localhost-key.pem");

        httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath, keyPath);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<User>()
    .AddEntityFrameworkStores<AppDbContext>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DevConnection")));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}
else if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
// app.UseFileServer();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    database.Database.Migrate();
}

app.MapCustomIdentityApi<User>();

app.UseHttpsRedirection();


#region Notebook
var notebooks = app.MapGroup("/notebooks").WithTags("Notebook").RequireAuthorization();

notebooks.MapGet("/", GetAllNotebooks)
    .Produces<List<NotebookDto>>(200);

notebooks.MapGet("/{notebookId}", GetNotebook)
    .Produces<NotebookDto>(200)
    .Produces(404);

notebooks.MapPost("/", CreateNotebook).Produces<NotebookDto>(201)
    .Produces(400);

notebooks.MapPut("/{notebookId}", UpdateNotebook)
    .Produces<NotebookDto>(204)
    .Produces(404);

notebooks.MapDelete("/{notebookId}", DeleteNotebook)
    .Produces<NotebookDto>(204)
    .Produces(404);


static async Task<Ok<List<NotebookDto>>> GetAllNotebooks(AppDbContext db, UserManager<User> user, ClaimsPrincipal claims)
{
    var userId = user.GetUserId(claims);

    var notebooks = await db.Notebooks
        .AsNoTracking()
        .Where(u => u.UserId == userId)
        .Select(n => new NotebookDto(n))
        .ToListAsync();

    return TypedResults.Ok(notebooks);
}

static async Task<Results<Ok<NotebookDto>, NotFound>> GetNotebook(AppDbContext db, UserManager<User> user, ClaimsPrincipal claims, int notebookId)
{
    var userId = user.GetUserId(claims);

    var notebook = await db.Notebooks
        .AsNoTracking()
        .Where(u => u.UserId == userId && u.Id == notebookId)
        .Select(n => new NotebookDto(n))
        .FirstOrDefaultAsync();

    return notebook != null ? TypedResults.Ok(notebook) : TypedResults.NotFound();
}

static async Task<Results<Created<NotebookDto>, BadRequest>> CreateNotebook(AppDbContext db, UserManager<User> user, ClaimsPrincipal claims, CreateNotebookDto request)
{
    var userId = user.GetUserId(claims);

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return TypedResults.BadRequest();
    }

    var notebook = new Notebook
    {
        Title = request.Title,
        UserId = userId,
    };

    db.Notebooks.Add(notebook);
    await db.SaveChangesAsync();

    var notebookDto = new NotebookDto(notebook);

    return TypedResults.Created($"/notebooks/{notebookDto.Id}", notebookDto);
}

static async Task<Results<NotFound, NoContent>> UpdateNotebook(AppDbContext db, UserManager<User> user, ClaimsPrincipal claims, CreateNotebookDto request, int notebookId)
{
    var userId = user.GetUserId(claims);

    var notebook = await db.Notebooks.FirstOrDefaultAsync(nb => nb.Id == notebookId && nb.UserId == userId);

    if (notebook != null)
    {
        if (notebook.Title != request.Title && !string.IsNullOrWhiteSpace(request.Title))
        {
            notebook.Title = request.Title;
            await db.SaveChangesAsync();
        }
    }
    else
        return TypedResults.NotFound();

    return TypedResults.NoContent();
}

static async Task<Results<NoContent, NotFound>> DeleteNotebook(AppDbContext db, UserManager<User> user, ClaimsPrincipal claims, int notebookId)
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

#endregion

#region Note
// 
#endregion


app.Run();
