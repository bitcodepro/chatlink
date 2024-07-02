using ChatLink.Models.Enums;

namespace ChatLink.Models.DTOs;

public class MessageDto
{
    public Guid MessageId { get; set; }

    public MessageType Type { get; set; }

    public required string Email { get; set; }

    public required string UserName { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string EncryptedMessage { get; set; } = string.Empty;
    public string DecryptedMessage { get; set; } = string.Empty;

    public DateTime MessageCreatedDt { get; set; } = DateTime.UtcNow;

    public Guid SessionId { get; set; }

    public bool IsCurrentUser { get; set; }
}
