using Backend.DTOs.Auth;

namespace Backend.Services.Auth
{
    public interface IGoogleAuthService
    {
        Task<GoogleRegisterResponseDto> RegisterAsync(GoogleRegisterRequestDto request);
        Task<GoogleLoginResponseDto> LoginAsync(GoogleLoginRequestDto request);
    }
}
