using Microsoft.AspNetCore.Identity;

namespace ChatLink.Models.Models;

public class User : IdentityUser
{
    public required string Login { get; set; }

    public string? Image { get; set; }

    public required bool CanShowStatus { get; set; }

    public bool AllowToSearch { get; set; }

    public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}