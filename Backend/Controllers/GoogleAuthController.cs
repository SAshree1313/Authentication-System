using Backend.DTOs.Auth;
using Backend.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/auth/google")]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IGoogleAuthService _service;

        public GoogleAuthController(IGoogleAuthService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(GoogleRegisterRequestDto request)
        {
            var result = await _service.RegisterAsync(request);
            return Ok(result);
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] GoogleLoginRequestDto request)
        {
            var result = await _service.LoginAsync(request);
            return Ok(result);
        }
    }
}
