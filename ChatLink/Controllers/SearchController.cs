using AutoMapper;
using ChatLink.Models.DTOs;
using ChatLink.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatLink.Controllers;

[Authorize]
[Route("api/chatlink/[controller]")]
[ApiController]
public class SearchController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IUserService _userService;
    private readonly IChatService _chatService;

    public SearchController(IMapper mapper, IUserService userService, IChatService chatService)
    {
        _mapper = mapper;
        _userService = userService;
        _chatService = chatService;
    }

    [Route("get-user")]
    [HttpPost]
    public async Task<IActionResult> GetUser([FromBody] string email)
    {
        var user = await _userService.GetUserByEmail(email);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<UserDto>(user));
    }

    [Route("get-current-user")]
    [HttpPost]
    public async Task<IActionResult> GetCurrentUser()
    {
        var email = User.FindFirst("sid")?.Value;

        if (email is null)
        {
            return NotFound();
        }

        var user = await _userService.GetUserByEmail(email);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<UserDto>(user));
    }

    [Route("get-contacts")]
    [HttpPost]
    public async Task<List<UserDto>> GetContacts()
    {
        var email = User.FindFirst("sid")?.Value;

        if (email is null)
        {
            return [];
        }

        var users = await _userService.GetContactsByEmail(email);

        return _mapper.Map<List<UserDto>>(users);
    }

    [Route("get-user-session-data")]
    [HttpPost]
    public async Task<List<UserSessionDataDto>> GetUserSessionData()
    {
        var email = User.FindFirst("sid")?.Value;

        if (email is null)
        {
            return [];
        }

        var userSessionData = await _chatService.GetUserSessionData(email);

        return userSessionData;
    }

    [Route("get-missed-messages")]
    [HttpPost]
    public async Task<List<MessageDto>> GetMissedMessages()
    {
        var email = User.FindFirst("sid")?.Value;
        if (email is null)
        {
            return [];
        }

        var userSessionData = await _chatService.GetMissedMessages(email);

        return userSessionData;
    }

    [Route("save-message/{sessionId}")]
    [HttpPost]
    public async Task<IActionResult> SaveMessage(Guid sessionId, [FromBody] MessageTinyDto message)
    {
        var email = User.FindFirst("sid")?.Value;
    
        if (email is null)
        {
            return BadRequest();
        }
    
        var result = await _chatService.SaveMessage(email, message);
    
        return result != null ? Ok() : BadRequest();
    }
}
