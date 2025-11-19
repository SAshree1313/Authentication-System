using Backend.DTOs.Passkey;

namespace Backend.Services.Passkey
{
    public interface IPasskeyService
    {
        //New Register methods
        Task<PasskeyRegisterBeginResponseDto> RegisterBeginAsync(PasskeyRegisterBeginRequestDto request);
        Task<PasskeyRegisterCompleteResponseDto> RegisterCompleteAsync(PasskeyRegisterCompleteRequestDto request);

        //Login methods
        Task<PasskeyLoginBeginResponseDto> LoginBeginAsync();
        Task<PasskeyLoginCompleteResponseDto> LoginCompleteAsync(PasskeyLoginCompleteRequestDto request);
    }
}
