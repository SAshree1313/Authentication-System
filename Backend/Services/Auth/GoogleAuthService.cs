using Backend.Data;
using Backend.DTOs.Auth;
using Backend.Exceptions;
using Backend.Helpers;
using Backend.Models;
using Backend.Services.Token;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Backend.Services.Auth
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;

        public GoogleAuthService(
            AppDbContext context,
            ITokenService tokenService,
            IConfiguration config,
            IMemoryCache cache)
        {
            _context = context;
            _tokenService = tokenService;
            _config = config;
            _cache = cache;
        }

        // ============================================================
        // GOOGLE REGISTRATION
        // - Creates user if needed
        // - Generates recovery code ONLY for brand-new users
        // - Links Google provider
        // ============================================================
        public async Task<GoogleRegisterResponseDto> RegisterAsync(GoogleRegisterRequestDto request)
        {
            var googleClientId = _config["Google:ClientId"]!;
            var googleUser = await GoogleTokenHelper.ValidateAsync(
                request.IdToken,
                googleClientId);

            // 1. Block duplicate Google identity
            var existingProvider = await _context.AuthProviders
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProviderName == "google" &&
                    p.ProviderSub == googleUser.Sub);

            if (existingProvider != null)
            {
                var user1 = await _context.Users.FindAsync(existingProvider.UserId);
                return new GoogleRegisterResponseDto
                {
                    IsNewUser = false,
                    AccessToken = _tokenService.GenerateToken(user1!)
                };
            }

            // 2. Find user by email
            var user = await _context.Users
                .Include(u => u.AuthProviders)
                .FirstOrDefaultAsync(u => u.Email == googleUser.Email);

            // 3. Prevent linking multiple Google accounts
            if (user != null && user.AuthProviders.Any(p => p.ProviderName == "google"))
                throw new ApiException("This account is already linked with Google. Please log in.");

            bool isNewUser = false;
            string? recoveryCode = null;

            await using var txn = await _context.Database.BeginTransactionAsync();
            try
            {
                // ----------------------------------------------------
                // CASE A: Brand-new user
                // ----------------------------------------------------
                if (user == null)
                {
                    isNewUser = true;
                    recoveryCode = RecoveryCodeHelper.GenerateRecoveryCode();

                    user = new User
                    {
                        Name = googleUser.Name ?? googleUser.Email,
                        Email = googleUser.Email,
                        EmailVerified = true,
                        RecoveryCodeHash = RecoveryCodeHelper.HashRecoveryCode(recoveryCode),
                        RecoveryCodeCreatedAt = DateTime.UtcNow,
                        TokenVersion = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                // ----------------------------------------------------
                // CASE B: Existing user → mark email verified if needed
                // ----------------------------------------------------
                else if (!user.EmailVerified)
                {
                    user.EmailVerified = true;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // ----------------------------------------------------
                // Link Google provider
                // ----------------------------------------------------
                _context.AuthProviders.Add(new AuthProvider
                {
                    UserId = user.Id,
                    ProviderName = "google",
                    ProviderSub = googleUser.Sub,
                    ProviderClaimsJson = JsonSerializer.Serialize(googleUser),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await txn.CommitAsync();
            }
            catch
            {
                await txn.RollbackAsync();
                throw;
            }

            return new GoogleRegisterResponseDto
            {
                IsNewUser = isNewUser,
                AccessToken = _tokenService.GenerateToken(user),
                RecoveryCode = recoveryCode
            };
        }

        // ============================================================
        // GOOGLE LOGIN
        // - Rate limited
        // - No recovery codes
        // - Auto-link if email matches
        // - Creates user only if truly new
        // ============================================================
        public async Task<GoogleLoginResponseDto> LoginAsync(GoogleLoginRequestDto request)
        {
            var googleClientId = _config["Google:ClientId"]!;

            GoogleTokenResult googleUser;
            try
            {
                googleUser = await GoogleTokenHelper.ValidateAsync(
                    request.IdToken,
                    googleClientId);
            }
            catch
            {
                // Token invalid → count as failed attempt (unknown email case)
                throw;
            }

            // ----------------------------------------------------
            // Rate limit check (by email)
            // ----------------------------------------------------
            if (LoginRateLimiterHelper.IsCooldownActive(
                    _cache,
                    googleUser.Email,
                    out var secondsLeft))
            {
                throw new ApiException($"COOLDOWN:{secondsLeft}");
            }

            try
            {
                // ----------------------------------------------------
                // 1. Login via existing Google provider
                // ----------------------------------------------------
                var provider = await _context.AuthProviders
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p =>
                        p.ProviderName == "google" &&
                        p.ProviderSub == googleUser.Sub);

                if (provider != null)
                {
                    // Successful login → reset rate limiter
                    LoginRateLimiterHelper.ResetFailCount(_cache, googleUser.Email);

                    return new GoogleLoginResponseDto
                    {
                        AccessToken = _tokenService.GenerateToken(provider.User!),
                        IsFirstLogin = false
                    };
                }

                // ----------------------------------------------------
                // 2. No provider → try find user by email
                // ----------------------------------------------------
                var user = await _context.Users
                    .Include(u => u.AuthProviders)
                    .FirstOrDefaultAsync(u => u.Email == googleUser.Email);

                bool isFirstLogin = false;
                string? recoveryCode = null;

                await using var txn = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ------------------------------------------------
                    // CASE A: First-ever user → generate recovery code
                    // ------------------------------------------------
                    if (user == null)
                    {
                        isFirstLogin = true;
                        recoveryCode = RecoveryCodeHelper.GenerateRecoveryCode();

                        user = new User
                        {
                            Name = googleUser.Name ?? googleUser.Email,
                            Email = googleUser.Email,
                            EmailVerified = true,
                            RecoveryCodeHash = RecoveryCodeHelper.HashRecoveryCode(recoveryCode),
                            RecoveryCodeCreatedAt = DateTime.UtcNow,
                            TokenVersion = 1,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.Users.Add(user);
                        await _context.SaveChangesAsync();
                    }
                    // ------------------------------------------------
                    // CASE B: Existing user → mark email as verified
                    // ------------------------------------------------
                    else if (!user.EmailVerified)
                    {
                        user.EmailVerified = true;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }

                    // ------------------------------------------------
                    // Link Google provider
                    // ------------------------------------------------
                    _context.AuthProviders.Add(new AuthProvider
                    {
                        UserId = user.Id,
                        ProviderName = "google",
                        ProviderSub = googleUser.Sub,
                        ProviderClaimsJson = JsonSerializer.Serialize(googleUser),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                    await txn.CommitAsync();
                }
                catch
                {
                    await txn.RollbackAsync();
                    throw;
                }

                // Successful login → reset rate limiter
                LoginRateLimiterHelper.ResetFailCount(_cache, googleUser.Email);

                return new GoogleLoginResponseDto
                {
                    AccessToken = _tokenService.GenerateToken(user),
                    IsFirstLogin = isFirstLogin,
                    RecoveryCode = recoveryCode
                };
            }
            catch
            {
                // Any failure increments rate limit
                LoginRateLimiterHelper.IncrementFailCount(_cache, googleUser.Email);
                throw;
            }
        }

    }
}
