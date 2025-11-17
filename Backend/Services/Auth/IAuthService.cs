using Backend.DTOs.Register;
using Backend.DTOs.Login;

namespace Backend.Services.Auth
{
    public interface IAuthService
    {
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    }
}