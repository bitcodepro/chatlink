using System.ComponentModel.DataAnnotations;

namespace ChatLink.Models.DTOs;

public class AuthDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }


    public string UserName { get; set; } = string.Empty;
}
