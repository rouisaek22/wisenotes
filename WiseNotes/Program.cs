using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WiseNotes;
using WiseNotes.Database;
using WiseNotes.Endpoints;
using WiseNotes.Models;
using WiseNotes.Services;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"🚀 Running in: {builder.Environment.EnvironmentName}");


if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(opt =>
    {
        opt.AddPolicy("DevelopmentPolicy", conf =>
        {
            conf.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
        });
    });
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<User>(configure =>
{
    configure.Password.RequiredUniqueChars = 0;
    configure.Password.RequireDigit = false;

}).AddEntityFrameworkStores<AppDbContext>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}
else if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("ProConnection")));
}

var app = builder.Build();

app.UseHttpsRedirection();

app.UseMiddleware<GlobalErrorHandlingMiddleware>();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Type = "https://example.com/errors/internal",
            Title = "An unexpected error occurred",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = "Please contact support if the problem persists.",
            Instance = context.Request.Path
        };

        problemDetails.Extensions.Add("traceId", context.TraceIdentifier);
        problemDetails.Extensions.Add("timestamp", DateTime.UtcNow);

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseFileServer();

if (app.Environment.IsDevelopment())
{
    // app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();

    // using var scope = app.Services.CreateScope();
    // var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // database.Database.Migrate();

    app.UseCors("DevelopmentPolicy");
}

app.MapCustomIdentityApi<User>();

NotebookEndpoints.Map(app);
NoteEndpoints.Map(app);

app.Run();
