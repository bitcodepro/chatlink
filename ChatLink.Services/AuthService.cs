using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ChatLink.Models.Auth;
using ChatLink.Models.Models;
using ChatLink.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace ChatLink.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IOptions<AuthOptions> _authOptions;

    public AuthService(UserManager<User> userManager, SignInManager<User> signInManager, IOptions<AuthOptions> authOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _authOptions = authOptions;
    }

    public static string GetSha256(string text)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(text);

        using SHA256 sha256 = SHA256.Create();

        byte[] hashBytes = sha256.ComputeHash(dataBytes);
        string hashString = Convert.ToHexString(hashBytes);

        return hashString;
    }

    public string GenerateJwt(User user)
    {
        var authParams = _authOptions.Value;

        var securityKey = authParams.GetSymmetricSecurityKey();
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sid, user.Email),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(authParams.TokenLifetime),
            Issuer = authParams.Issuer,
            Audience = authParams.Audience,
            SigningCredentials = credentials,
            EncryptingCredentials = new EncryptingCredentials(securityKey, SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes128CbcHmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var encryptedJwt = tokenHandler.WriteToken(securityToken);

        return encryptedJwt;
    }

    public async Task<string> LoginUser(string email, string password)
    {
        var user = await GetAuthenticationUser(email);

        if (user == null)
        {
            return string.Empty;
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, false, false);

        return result.Succeeded ? GenerateJwt(user) : string.Empty;
    }

    public async Task<bool> RegisterUser(string email, string password, string userName)
    {
        var user = await GetAuthenticationUser(email);

        if (user != null)
        {
            return false;
        }

        user = new User
        {
            Login = email,
            UserName = string.IsNullOrWhiteSpace(userName) ? email : userName.Trim(),
            Email = email,
            CanShowStatus = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            //TODO: add logger here
            foreach (var error in result.Errors)
            {
                Console.WriteLine(error.Description);
            }
        }

        return result.Succeeded;
    }

    private async Task<User?> GetAuthenticationUser(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }
}
