// using Backend.Data;
// using Backend.DTOs.Register;
// using Backend.Models;
// using Backend.Services.Token;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.EntityFrameworkCore;
// using Backend.DTOs.Login;
// using Backend.Exceptions;
// using Backend.DTOs.Passkey;

// namespace Backend.Services.Auth
// {
//     public class AuthService : IAuthService
//     {
//         private readonly AppDbContext _context;

//         // Keeper of old password hashing (kept only to avoid breaking DI)
//         private readonly PasswordHasher<User> _passwordHasher;
//         private readonly ITokenService _tokenService;

//         public AuthService(AppDbContext context, ITokenService tokenService)
//         {
//             _context = context;
//             _tokenService = tokenService;
//             _passwordHasher = new PasswordHasher<User>(); // NOT USED 
//         }

//         public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
//         {
//             // 1️⃣ Check if email exists
//             var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

//             if (existingUser != null)
//                 throw new DuplicateEmailException("Email already exists.");

//             // 2️⃣ Create user
//             var newUser = new User
//             {
//                 Name = request.Name,
//                 Email = request.Email
//             };

//             // Note: Password hashing is commented out for passkey-only authentication
//             // 3️⃣ Hash password
//             //newUser.PasswordHash = _passwordHasher.HashPassword(newUser, request.Password);

//             // 4️⃣ Save user
//             _context.Users.Add(newUser);
//             await _context.SaveChangesAsync();

//             // 5️⃣ Generate token
//             var token = _tokenService.GenerateToken(newUser);

//             // 6️⃣ Return safe DTO
//             return new RegisterResponseDto
//             {
//                 Id = newUser.Id,
//                 Name = newUser.Name,
//                 Email = newUser.Email,
//                 Token = token
//             };
//         }

//         public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
//         {
//             // 1️⃣ Find user by email
//             var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

//             if (user == null)
//                 throw new UserNotFoundException("User not found.");
            
//             // Note: Password verification is commented out for passkey-only authentication
//             // 2️⃣ Verify password
//             // var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
//             // if (result != PasswordVerificationResult.Success)
//             //     throw new InvalidCredentialsException("Invalid email or password.");

//             // 3️⃣ Generate JWT
//             var token = _tokenService.GenerateToken(user);

//             // 4️⃣ Return response
//             return new LoginResponseDto
//             {
//                 Id = user.Id,
//                 Name = user.Name,
//                 Email = user.Email,
//                 Token = token
//             };
//         }

        // public async Task<UserProfileResponseDto> GetProfileAsync(int userId)
        // {
        //     var user = await _context.Users
        //         .Include(u => u.WebAuthnCredentials)
        //         .FirstOrDefaultAsync(u => u.Id == userId);

        //     if (user == null)
        //         throw new UserNotFoundException("User not found.");

        //     return new UserProfileResponseDto
        //     {
        //         Id = user.Id,
        //         Name = user.Name,
        //         Email = user.Email,
        //         //HasPasskey = user.WebAuthnCredentials.Any()
        //     };
        // }

//     }
// }
