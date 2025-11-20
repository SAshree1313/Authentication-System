using Backend.DTOs.Register;
using Backend.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs.Login;
using Backend.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // -----------------------------------------------------------
        //  POST: /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            int? userId = JwtHelper.GetUserId(User);
            
            if (userId == null)
                return Unauthorized();

            var profile = await _authService.GetProfileAsync(userId.Value);

            return Ok(profile);
        }

    }
}
