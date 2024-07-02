using ChatLink.Models.DTOs;
using ChatLink.Models.Models;

namespace ChatLink.Services.Interfaces;

public interface IChatService
{
    public Task<bool> CreateChat(string user1, string user2);

    public Task<List<UserSessionDataDto>> GetUserSessionData(string email);

    public Task<List<MessageDto>> GetMissedMessages(string email);
    public Task<Guid?> SaveMessage(string email, MessageTinyDto messageTinyDto);
    public Task<Message?> GetMessage(Guid messageId);
    public Task<MessageDto?> GetMessageDto(string currentUserEmail, Guid messageId);
}
