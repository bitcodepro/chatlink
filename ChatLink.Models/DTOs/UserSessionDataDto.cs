using ChatLink.Models.Models;

namespace ChatLink.Models.DTOs;

public class UserSessionDataDto
{
    public required Session Session { get; set; }

    public required UserDto User { get; set; }
}
