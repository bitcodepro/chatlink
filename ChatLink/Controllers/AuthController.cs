using ChatLink.Models.DTOs;
using ChatLink.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChatLink.Controllers;

[Route("api/chatlink/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [Route("login")]
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] AuthDto authDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        authDto.UserName = authDto.Email;

        var accessToken = await _authService.LoginUser(authDto.Email, authDto.Password);

        if (!string.IsNullOrEmpty(accessToken))
        {
            return Ok(new
            {
                access_token = accessToken
            });
        }

        return Unauthorized();
    }

    [Route("register")]
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] AuthDto authDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }

        bool hasUserRegistered = await _authService.RegisterUser(authDto.Email, authDto.Password, authDto.UserName);

        if (hasUserRegistered)
        {
            return Ok();
        }

        return BadRequest("Something went wrong");
    }
}
