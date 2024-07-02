using ChatLink.Models.Models;

namespace ChatLink.Services.Interfaces;

public interface IUserService
{
    public Task<User?> GetUserByEmail(string email);

    public Task<List<User>> GetContactsByEmail(string email);
}