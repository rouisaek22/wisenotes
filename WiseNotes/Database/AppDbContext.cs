using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WiseNotes.Models;

namespace WiseNotes.Database;

public class AppDbContext : IdentityDbContext<IdentityUser>
{       
    public DbSet<Notebook> Notebooks { get; set; }
    public DbSet<Note> Notes { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}