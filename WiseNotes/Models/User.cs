using Microsoft.AspNetCore.Identity;

namespace WiseNotes.Models;

public class User : IdentityUser
{
    public List<Notebook> Notebooks { get; set; } = [];
}
