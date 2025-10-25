using Microsoft.AspNetCore.Identity;

namespace Notes.Api.Models;

public class User : IdentityUser
{
    public List<Notebook> Notebooks { get; set; } = [];
}
