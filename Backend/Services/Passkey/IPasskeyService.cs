using Backend.DTOs.Passkey;
using Backend.DTOs.Recovery;
using Backend.DTOs.MultiDevice;
using System.Threading.Tasks;

namespace Backend.Services.Passkey
{
    public interface IPasskeyService
    {
        // -----------------------------
        // Registration
        // -----------------------------
        Task<PasskeyRegisterBeginResponseDto> RegisterBeginAsync(PasskeyRegisterBeginRequestDto request);
        Task<PasskeyRegisterCompleteResponseDto> RegisterCompleteAsync(PasskeyRegisterCompleteRequestDto request);

        // -----------------------------
        // Login
        // -----------------------------
        Task<PasskeyLoginBeginResponseDto> LoginBeginAsync(PasskeyLoginBeginRequestDto request);
        Task<PasskeyLoginCompleteResponseDto> LoginCompleteAsync(PasskeyLoginCompleteRequestDto request);

        // -----------------------------
        // Recovery
        // -----------------------------
        Task<PasskeyRecoveryBeginResponseDto> RecoveryBeginAsync(PasskeyRecoveryBeginRequestDto request);
        Task<PasskeyRecoveryVerifyCodeResponseDto> RecoveryVerifyCodeAsync(PasskeyRecoveryVerifyCodeRequestDto request);
        Task<PasskeyRecoveryCompleteResponseDto> RecoveryCompleteAsync(PasskeyRecoveryCompleteRequestDto request);

        // -----------------------------
        // Profile
        // -----------------------------
        Task<UserProfileResponseDto> GetProfileAsync(int userId);

        /// -------------------------
        // Multi-Device Endpoints
        // -------------------------
        Task<PasskeyDeviceListResponseDto> GetDevicesAsync(int userId);
        Task<PasskeyDeviceDto> UpdateDeviceNameAsync(int userId, string credentialId, string deviceName);
        Task<DeleteDeviceResponseDto> DeleteDeviceAsync(int userId, string credentialId);

        // -------------------------
        // Add Device
        // -------------------------
        Task<PasskeyRegisterBeginResponseDto> AddDeviceBeginAsync(int userId);
        Task<PasskeyRegisterCompleteResponseDto> AddDeviceCompleteAsync(int userId, PasskeyRegisterCompleteRequestDto request);

        // -------------------------
        // Delete Account
        // -------------------------
        Task DeleteAccountAsync(int userId);


    }
}