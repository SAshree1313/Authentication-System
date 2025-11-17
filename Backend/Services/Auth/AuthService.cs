using Backend.Data;
using Backend.DTOs.Register;
using Backend.Models;
using Backend.Services.Token;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Backend.DTOs.Login;
using Backend.Exceptions;

namespace Backend.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;

        public AuthService(AppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            // 1️⃣ Check if email exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
                throw new Exception("Email already exists.");

            // 2️⃣ Create user
            var newUser = new User
            {
                Name = request.Name,
                Email = request.Email
            };

            // 3️⃣ Hash password
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);

            // 4️⃣ Save user
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // 5️⃣ Generate token
            var token = _tokenService.GenerateToken(newUser);

            // 6️⃣ Return safe DTO
            return new RegisterResponseDto
            {
                Id = newUser.Id,
                Name = newUser.Name,
                Email = newUser.Email,
                Token = token
            };
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // 1️⃣ Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                throw new UserNotFoundException("User not found.");

            // 2️⃣ Verify password
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result != PasswordVerificationResult.Success)
                throw new InvalidCredentialsException("Invalid email or password.");

            // 3️⃣ Generate JWT
            var token = _tokenService.GenerateToken(user);

            // 4️⃣ Return response
            return new LoginResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = token
            };
        }
    }
}
