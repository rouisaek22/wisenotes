using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WiseNotes;
using WiseNotes.Database;
using WiseNotes.Endpoints;
using WiseNotes.Models;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddIdentityApiEndpoints<User>(configure =>
{
    configure.Password.RequiredUniqueChars = 0;
    configure.Password.RequireDigit = false;

}).AddEntityFrameworkStores<AppDbContext>();

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
app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseFileServer();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();

    // using var scope = app.Services.CreateScope();
    // var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // database.Database.Migrate();
}

app.MapCustomIdentityApi<User>();

NotebookEndpoints.Map(app);

app.Run();
