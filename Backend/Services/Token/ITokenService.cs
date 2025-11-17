using Backend.Models;

namespace Backend.Services.Token
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
