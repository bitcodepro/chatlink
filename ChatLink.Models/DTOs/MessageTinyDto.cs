using ChatLink.Models.Enums;

namespace ChatLink.Models.DTOs;

public class MessageTinyDto
{
    public Guid SessionId { get; set; }

    public MessageType Type { get; set; }

    public string EncryptedMessage { get; set; } = string.Empty;
}
