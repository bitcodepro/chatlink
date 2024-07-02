namespace ChatLink.Models.Models;

public class UserSession
{
    public string UserId { get; set; }
    public User User { get; set; }

    public Guid SessionId { get; set; }
    public Session Session { get; set; }
}