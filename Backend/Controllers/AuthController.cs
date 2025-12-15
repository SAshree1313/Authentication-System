//using Backend.DTOs.Register;
//using Backend.Services.Auth;
using Backend.Services.Passkey; 
using Microsoft.AspNetCore.Mvc;
//using Backend.DTOs.Login;
using Backend.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IPasskeyService _passkeyService;

        public AuthController(IPasskeyService passkeyService)
        {
            _passkeyService = passkeyService;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            int? userId = JwtHelper.GetUserId(User);
            
            if (userId == null)
                return Unauthorized();

            var profile = await _passkeyService.GetProfileAsync(userId.Value);
            //var profile = await _authService.GetProfileAsync(userId.Value);

            return Ok(profile);
        }

    }
}
