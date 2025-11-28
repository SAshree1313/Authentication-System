using Backend.DTOs.Passkey;
using Backend.DTOs.Recovery;
using Backend.Services.Passkey;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/passkey")]
    public class PasskeyController : ControllerBase
    {
        private readonly IPasskeyService _passkeyService;

        public PasskeyController(IPasskeyService passkeyService)
        {
            _passkeyService = passkeyService;
        }

        // -----------------------------------------------------------
        // REGISTRATION
        // -----------------------------------------------------------
        [HttpPost("register/begin")]
        public async Task<IActionResult> RegisterBegin(
            [FromBody] PasskeyRegisterBeginRequestDto request)
        {
            return Ok(await _passkeyService.RegisterBeginAsync(request));
        }

        [HttpPost("register/complete")]
        public async Task<IActionResult> RegisterComplete(
            [FromBody] PasskeyRegisterCompleteRequestDto request)
        {
            return Ok(await _passkeyService.RegisterCompleteAsync(request));
        }

        // -----------------------------------------------------------
        // LOGIN
        // -----------------------------------------------------------
        [HttpPost("login/begin")]
        public async Task<IActionResult> LoginBegin(
            [FromBody] PasskeyLoginBeginRequestDto request)
        {
            return Ok(await _passkeyService.LoginBeginAsync(request));
        }

        [HttpPost("login/complete")]
        public async Task<IActionResult> LoginComplete(
            [FromBody] PasskeyLoginCompleteRequestDto request)
        {
            return Ok(await _passkeyService.LoginCompleteAsync(request));
        }

        // -----------------------------------------------------------
        // RECOVERY
        // -----------------------------------------------------------
        [HttpPost("recovery/begin")]
        public async Task<IActionResult> RecoveryBegin(
            [FromBody] PasskeyRecoveryBeginRequestDto request)
        {
            return Ok(await _passkeyService.RecoveryBeginAsync(request));
        }

        [HttpPost("recovery/verify-code")]
        public async Task<IActionResult> RecoveryVerifyCode(
            [FromBody] PasskeyRecoveryVerifyCodeRequestDto request)
        {
            return Ok(await _passkeyService.RecoveryVerifyCodeAsync(request));
        }

        [HttpPost("recovery/complete")]
        public async Task<IActionResult> RecoveryComplete(
            [FromBody] PasskeyRecoveryCompleteRequestDto request)
        {
            return Ok(await _passkeyService.RecoveryCompleteAsync(request));
        }

        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            // Assuming you have a JWT-based auth and UserId is in claims
            var userIdClaim = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            await _passkeyService.DeleteAccountAsync(userId);
            return Ok(new { message = "Account deleted successfully" });
        }

    }
}
