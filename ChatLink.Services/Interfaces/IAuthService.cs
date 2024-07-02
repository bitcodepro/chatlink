using ChatLink.Models.Models;

namespace ChatLink.Services.Interfaces;

public interface IAuthService
{
    public string GenerateJwt(User user);

    public Task<string> LoginUser(string email, string password);

    public Task<bool> RegisterUser(string email, string password, string userName);
}