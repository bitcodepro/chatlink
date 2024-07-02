using ChatLink.Models.Enums;

namespace ChatLink.Models.Models;

public class Message
{
    public Guid Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public MessageType Type { get; set; }

    public bool IsDelivered { get; set; }

    public bool IsRead { get; set; }

    public Guid SessionId { get; set; }
    public Session Session { get; set; }

    public string Email { get; set; } = string.Empty; // who sent message

    public DateTime CreatedDt { get; set; }
}
