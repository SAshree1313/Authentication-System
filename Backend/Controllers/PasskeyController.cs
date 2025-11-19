using Backend.DTOs.Passkey;
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
        // POST: /api/passkey/register/begin
        // Step 1: Generate challenge + FIDO options
        // -----------------------------------------------------------
        [HttpPost("register/begin")]
        public async Task<IActionResult> RegisterBegin([FromBody] PasskeyRegisterBeginRequestDto request)
        {
            var result = await _passkeyService.RegisterBeginAsync(request);
            return Ok(result);
        }

        // -----------------------------------------------------------
        // POST: /api/passkey/register/complete
        // Step 2: Verify attestation + store credential
        // -----------------------------------------------------------
        [HttpPost("register/complete")]
        public async Task<IActionResult> RegisterComplete([FromBody] PasskeyRegisterCompleteRequestDto request)
        {
            var result = await _passkeyService.RegisterCompleteAsync(request);
            return Ok(result);
        }

        // -----------------------------------------------------------
        // LOGIN BEGIN
        // POST: /api/passkey/login/begin
        // Step 1: Generate assertion options for passwordless login
        // -----------------------------------------------------------
        [HttpPost("login/begin")]
        public async Task<IActionResult> LoginBegin()
        {
            var result = await _passkeyService.LoginBeginAsync();
            return Ok(result);
        }

        // -----------------------------------------------------------
        // LOGIN COMPLETE
        // POST: /api/passkey/login/complete
        // Step 2: Validate assertion and issue JWT
        // -----------------------------------------------------------
        [HttpPost("login/complete")]
        public async Task<IActionResult> LoginComplete([FromBody] PasskeyLoginCompleteRequestDto request)
        {
            var result = await _passkeyService.LoginCompleteAsync(request);
            return Ok(result);
        }
    }
}
