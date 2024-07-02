using ChatLink.Models;
using ChatLink.Models.Models;
using ChatLink.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChatLink.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;

    public UserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<List<User>> GetContactsByEmail(string email)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user is null)
        {
            return [];
        }

        var sessionIds = await _dbContext.UserSessions
            .Where(x => x.UserId == user.Id)
            .Select(x => x.SessionId)
            .ToListAsync();

        if (sessionIds.Count == 0)
        {
            return new List<User>();
        }

        var userIds = await _dbContext.UserSessions.
            Where(x => sessionIds.Contains(x.SessionId) && x.UserId != user.Id)
            .Select(x => x.UserId)
            .ToListAsync();

        return await _dbContext.Users.Where(x => userIds.Contains(x.Id)).ToListAsync();
    }
}
