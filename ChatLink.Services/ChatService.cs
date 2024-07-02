using AutoMapper;
using ChatLink.Models;
using ChatLink.Models.DTOs;
using ChatLink.Models.Enums;
using ChatLink.Models.Models;
using ChatLink.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChatLink.Services;

public class ChatService : IChatService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public ChatService(AppDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<bool> CreateChat(string user1, string user2)
    {
        var userEmails = new List<string> {user1, user2};
        var users = await _dbContext.Users.Where(x => userEmails.Contains(x.Email)).ToListAsync();

        if (users.Count != 2)
        {
            return false;
        }

        var session = new Session
        {
            Title = "New Chat",
            CreatedDt = DateTime.UtcNow,
        };

        await _dbContext.AddAsync(session);

        foreach (var user in users)
        {
            var userSession = new UserSession
            {
                User = user,
                Session = session,
                SessionId = session.Id,
                UserId = user.Id
            };

            await _dbContext.AddAsync(userSession);
        }

        await _dbContext.SaveChangesAsync();

        return  true;
    }

    public async Task<List<UserSessionDataDto>> GetUserSessionData(string email)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user is null)
        {
            return [];
        }

        var userSessions = await _dbContext.UserSessions
            .Where(x => x.UserId == user.Id)
            .ToListAsync();

        if (userSessions.Count == 0)
        {
            return [];
        }

        var sessionIds = userSessions.Select(x => x.SessionId).ToList();

        var userIds = await _dbContext.UserSessions.
            Where(x => sessionIds.Contains(x.SessionId) && x.UserId != user.Id)
            .Select(x => x.UserId)
            .ToListAsync();

        var users = await _dbContext.Users.Where(x => userIds.Contains(x.Id)).ToListAsync();
        var sessions = await _dbContext.Sessions.Where(x => sessionIds.Contains(x.Id)).ToListAsync();
        userSessions = await _dbContext.UserSessions
            .Where(x => sessionIds.Contains(x.SessionId) && x.UserId != user.Id)
            .ToListAsync();

        var userSessionData = new List<UserSessionDataDto>();

        foreach (var userSession in userSessions)
        {
            userSessionData.Add(new UserSessionDataDto
            {
                User = _mapper.Map<UserDto>(users.Find(x => x.Id == userSession.UserId)),
                Session = sessions.Find(x => x.Id == userSession.SessionId)!
            });
        }

        return userSessionData;
    }

    public async Task<List<MessageDto>> GetMissedMessages(string email)
    {
        var user = await _dbContext.Users
            .Include(x => x.UserSessions).ThenInclude(y => y.Session)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user is null)
        {
            return [];
        }

        var sessionIds = user.UserSessions.Select(x => x.SessionId).ToList();
        var userIds = await _dbContext.UserSessions
            .Where(x => sessionIds.Contains(x.SessionId) && x.UserId != user.Id)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();

        if (sessionIds.Count == 0 || userIds.Count == 0)
        {
            return [];
        }

        var users = await _dbContext.Users.Where(x => userIds.Distinct().Contains(x.Id)).ToListAsync();

        var messages = await _dbContext.Messages
            .Where(x => sessionIds.Contains(x.SessionId) && !x.IsDelivered && x.Email != email)
            .OrderBy(x => x.CreatedDt)
            .ToListAsync();

        var result = new List<MessageDto>();

        foreach (var message in messages)
        {
            var u = users.Find(x => x.Email == message.Email)!;

            result.Add(new MessageDto
            {
                Email = u.Email!,
                UserName = u.UserName!,
                SessionId = message.SessionId,
                Type = message.Type,
                EncryptedMessage = message.Content,
                ImageUrl = "",
                IsCurrentUser = false,
                MessageCreatedDt = message.CreatedDt,
                MessageId = message.Id
            });
        }

        return result;
    }

    public async Task<Guid?> SaveMessage(string email, MessageTinyDto messageTinyDto)
    {
        var user = await _dbContext.Users
            .Include(x => x.UserSessions).ThenInclude(y => y.Session)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user is null)
        {
            return null;
        }

        Session? session = (from userSession in user.UserSessions where userSession.Session.Id == messageTinyDto.SessionId select userSession.Session).FirstOrDefault();

        if (session is null)
        {
            return null;
        }

        var m = new Message
        {
            Email = email,
            SessionId = messageTinyDto.SessionId,
            Content = messageTinyDto.EncryptedMessage,
            CreatedDt = DateTime.UtcNow,
            Session = session,
            Type = messageTinyDto.Type
        };
        session.Messages.Add(m);

        await _dbContext.SaveChangesAsync();

        return m.Id;
    }

    public async Task<Message?> GetMessage(Guid messageId)
    {
        return await _dbContext.Messages.FirstOrDefaultAsync(x => x.Id == messageId);
    }

    public async Task<MessageDto?> GetMessageDto(string currentUserEmail, Guid messageId)
    {
        var message = await GetMessage(messageId);

        if (message is null)
        {
            return null;
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == message.Email);

        if (user is null)
        {
            return null;
        }

        return new MessageDto
        {
            Email = message.Email,
            SessionId = message.SessionId,
            Type = message.Type,
            UserName = user.UserName!,
            EncryptedMessage = message.Content,
            MessageCreatedDt = message.CreatedDt,
            MessageId = message.Id
        };
    }
}
