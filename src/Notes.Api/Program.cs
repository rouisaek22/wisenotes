using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Net;
using Notes.Api;
using Notes.Api.Database;
using Notes.Api.Endpoints;
using Notes.Api.Models;

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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "ToDo API",
        Description = "An ASP.NET Core Web API for managing Notes",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Example Contact",
            Url = new Uri("https://example.com/contact")
        },
        License = new OpenApiLicense
        {
            Name = "Example License",
            Url = new Uri("https://example.com/license")
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

});

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<User>(configure =>
{
    configure.Password.RequiredUniqueChars = 0;
    configure.Password.RequireDigit = false;

}).AddEntityFrameworkStores<AppDbContext>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<AppDbContext>(
        opt => opt.UseSqlite("Data Source=wise.db"));

    // builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}
else if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

var app = builder.Build();

app.UseHttpsRedirection();

// For testing custom global error handling middleware
// app.UseMiddleware<GlobalErrorHandlingMiddleware>();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var contextFeature = context.Features.Get<IExceptionHandlerFeature>();

        if (contextFeature is not null)
        {
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
        }
    });
});

if (app.Environment.IsDevelopment())
{
    // app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    if (await um.FindByEmailAsync("dev@local") is null)
    {
        var u = new User { UserName = "dev@local", Email = "dev@local", EmailConfirmed = true };
        await um.CreateAsync(u, "Password123!");
    }

    app.UseCors("DevelopmentPolicy");
}

app.MapCustomIdentityApi<User>();

NotebookEndpoints.Map(app);
NoteEndpoints.Map(app);

// Test endpoint to demonstrate error handling
app.MapGet("/demo", () =>
{
    throw new ArgumentOutOfRangeException("Demo Exception");
});

app.Run();
