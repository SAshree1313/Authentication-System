using Backend.Data;
using Backend.DTOs.Passkey;
using Backend.DTOs.Recovery;
using Backend.DTOs.MultiDevice;
using Backend.Exceptions;
using Backend.Helpers;
using Backend.Models;
using Backend.Services.Token;
using Fido2NetLib;
using Fido2NetLib.Objects;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace Backend.Services.Passkey
{
    public class PasskeyService : IPasskeyService
    {
        private readonly AppDbContext _context;
        private readonly Fido2 _fido2;
        private readonly IMemoryCache _cache;
        private readonly ITokenService _tokenService;
        private readonly IValidator<PasskeyRegisterBeginRequestDto> _registerValidator;
        private readonly IValidator<PasskeyLoginBeginRequestDto> _loginValidator;

        private static readonly TimeSpan _challengeTtl = TimeSpan.FromMinutes(5);

        public PasskeyService(
            AppDbContext context,
            Fido2 fido2,
            IMemoryCache cache,
            ITokenService tokenService,
            IValidator<PasskeyRegisterBeginRequestDto> registerValidator,
            IValidator<PasskeyLoginBeginRequestDto> loginValidator)
        {
            _context = context;
            _fido2 = fido2;
            _cache = cache;
            _tokenService = tokenService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
        }

        // -------------------------------------------------------
        // Internal Session Objects
        // -------------------------------------------------------
        private class RegistrationSession
        {
            public CredentialCreateOptions Options { get; set; } = null!;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        private class LoginSession
        {
            public AssertionOptions Options { get; set; } = null!;
            public int UserId { get; set; }
        }

        private class RecoverySession
        {
            
            public int Step { get; set; }      // 1 = verify code, 2 = register passkey
            public int UserId { get; set; }
            public string Email { get; set; } = string.Empty;
            public CredentialCreateOptions? Options { get; set; }  // Only used in Step 2
        }

        // -------------------------------------------------------
        // PASSKEY REGISTRATION
        // -------------------------------------------------------
        public async Task<PasskeyRegisterBeginResponseDto> RegisterBeginAsync(PasskeyRegisterBeginRequestDto request)
        {
            var validation = await _registerValidator.ValidateAsync(request);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new DuplicateEmailException("Email already exists.");

            var fidoUser = new Fido2User
            {
                Id = Guid.NewGuid().ToByteArray(),
                Name = request.Email,
                DisplayName = request.Name
            };

            var options = _fido2.RequestNewCredential(fidoUser, new List<PublicKeyCredentialDescriptor>());

            var challengeId = Guid.NewGuid().ToString();
            _cache.Set(challengeId, new RegistrationSession
            {
                Options = options,
                Name = request.Name,
                Email = request.Email
            }, _challengeTtl);

            return new PasskeyRegisterBeginResponseDto
            {
                Options = options,
                ChallengeId = challengeId
            };
        }

        public async Task<PasskeyRegisterCompleteResponseDto> RegisterCompleteAsync(PasskeyRegisterCompleteRequestDto request)
        {
            if (!_cache.TryGetValue<RegistrationSession>(request.ChallengeId, out var session))
                throw new NotFoundException("Passkey challenge expired.");

            // Optional: remove challenge to prevent replay. You already did this; safe to do before attestation.
            _cache.Remove(request.ChallengeId);

            var attestationResponse = new AuthenticatorAttestationRawResponse
            {
                Id = Base64UrlHelper.Decode(request.RawId),
                RawId = Base64UrlHelper.Decode(request.RawId),
                Type = PublicKeyCredentialType.PublicKey,
                Response = new AuthenticatorAttestationRawResponse.ResponseData
                {
                    AttestationObject = Base64UrlHelper.Decode(request.Response.AttestationObject),
                    ClientDataJson = Base64UrlHelper.Decode(request.Response.ClientDataJSON)
                }
            };

            var makeResult = await _fido2.MakeNewCredentialAsync(
                attestationResponse,
                session.Options!,
                async (args, ct) =>
                {
                    var id = Base64UrlHelper.Encode(args.CredentialId);
                    return !await _context.WebAuthnCredentials.AnyAsync(c => c.CredentialId == id);
                });

            if (makeResult?.Result == null)
                throw new Exception("Attestation failed."); // consider a more specific exception

            // Double-check that email hasn't been registered between begin and complete
            if (await _context.Users.AnyAsync(u => u.Email == session.Email))
                throw new DuplicateEmailException("Email already exists.");

            // Use transaction to ensure user + credential are saved atomically
            await using var txn = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create user
                var recoveryCode = RecoveryCodeHelper.GenerateRecoveryCode();

                var user = new User
                {
                    Name = session.Name,
                    Email = session.Email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    RecoveryCodeHash = RecoveryCodeHelper.HashRecoveryCode(recoveryCode),
                    RecoveryCodeCreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Save credential with optional device name
                var credential = new WebAuthnCredential
                {
                    UserId = user.Id,
                    CredentialId = Base64UrlHelper.Encode(makeResult.Result.CredentialId),
                    PublicKey = Base64UrlHelper.Encode(makeResult.Result.PublicKey),
                    SignCount = (int)makeResult.Result.Counter,
                    CreatedAt = DateTime.UtcNow,
                    DeviceName = string.IsNullOrWhiteSpace(request.DeviceName) ? null : request.DeviceName
                };

                _context.WebAuthnCredentials.Add(credential);
                await _context.SaveChangesAsync();

                // Commit transaction
                await txn.CommitAsync();

                var token = _tokenService.GenerateToken(user);

                return new PasskeyRegisterCompleteResponseDto
                {
                    UserId = user.Id,
                    CredentialId = credential.CredentialId,
                    Token = token,
                    RecoveryCode = recoveryCode,
                    Success = true,
                    Message = "Passkey registered successfully."
                };
            }
            catch
            {
                await txn.RollbackAsync();
                throw; // bubble up so middleware will convert into proper HTTP response
            }
        }

        // -------------------------------------------------------
        // PASSKEY LOGIN
        // -------------------------------------------------------
        public async Task<PasskeyLoginBeginResponseDto> LoginBeginAsync(PasskeyLoginBeginRequestDto request)
        {
            var validation = await _loginValidator.ValidateAsync(request);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            // ============================
            // COOLDOWN CHECK (NEW)
            // ============================
            if (LoginRateLimiterHelper.IsCooldownActive(_cache, request.Email, out var secs))
                throw new ApiException($"COOLDOWN:{secs}");

            var user = await _context.Users
                .Include(u => u.WebAuthnCredentials)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                // Count fails for unknown email as well
                LoginRateLimiterHelper.IncrementFailCount(_cache, request.Email);
                throw new UserNotFoundException("User not found.");
            }

            if (!user.WebAuthnCredentials.Any())
            {
                LoginRateLimiterHelper.IncrementFailCount(_cache, request.Email);
                throw new InvalidOperationException("No passkeys registered.");
            }

            var allowedCreds = user.WebAuthnCredentials
                .Select(c => new PublicKeyCredentialDescriptor(Base64UrlHelper.Decode(c.CredentialId)))
                .ToList();

            var options = _fido2.GetAssertionOptions(allowedCreds, UserVerificationRequirement.Required);

            var challengeId = Guid.NewGuid().ToString();
            _cache.Set(challengeId, new LoginSession
            {
                Options = options,
                UserId = user.Id
            }, _challengeTtl);

            return new PasskeyLoginBeginResponseDto
            {
                Options = options,
                ChallengeId = challengeId
            };
        }

        public async Task<PasskeyLoginCompleteResponseDto> LoginCompleteAsync(PasskeyLoginCompleteRequestDto request)
        {
            if (!_cache.TryGetValue<LoginSession>(request.ChallengeId, out var session))
                throw new NotFoundException("Login session expired.");

            _cache.Remove(request.ChallengeId);

            var credentialIdString = Base64UrlHelper.Encode(Base64UrlHelper.Decode(request.RawId));

            var credential = await _context.WebAuthnCredentials
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CredentialId == credentialIdString);

            if (credential == null || credential.UserId != session.UserId)
            {
                // Count failed attempts
                var email = await _context.Users.Where(u => u.Id == session.UserId).Select(u => u.Email).FirstOrDefaultAsync();
                if (email != null)
                    LoginRateLimiterHelper.IncrementFailCount(_cache, email);

                throw new InvalidCredentialsException("Invalid credentials.");
            }

            // Verify FIDO2 assertion...
            var assertion = new AuthenticatorAssertionRawResponse 
            { Id = Base64UrlHelper.Decode(request.RawId),
                RawId = Base64UrlHelper.Decode(request.RawId),
                Type = PublicKeyCredentialType.PublicKey,
                Response = new AuthenticatorAssertionRawResponse.AssertionResponse
                {
                    AuthenticatorData = Base64UrlHelper.Decode(request.Response.AuthenticatorData),
                    ClientDataJson = Base64UrlHelper.Decode(request.Response.ClientDataJSON),
                    Signature = Base64UrlHelper.Decode(request.Response.Signature),
                    UserHandle = string.IsNullOrWhiteSpace(request.Response.UserHandle)
                        ? null
                        : Base64UrlHelper.Decode(request.Response.UserHandle)
                } };

            var result = await _fido2.MakeAssertionAsync(
                assertion,
                session.Options,
                Base64UrlHelper.Decode(credential.PublicKey),
                (uint)credential.SignCount,
                (handle, id) => Task.FromResult(true));

            if (result?.CredentialId == null)
            {
                LoginRateLimiterHelper.IncrementFailCount(_cache, credential.User!.Email);
                throw new InvalidCredentialsException("Assertion failed.");
            }

            // SUCCESS → RESET FAIL COUNT
            LoginRateLimiterHelper.ResetFailCount(_cache, credential.User!.Email);

            credential.SignCount = (int)result.Counter;
            credential.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(credential.User);

            return new PasskeyLoginCompleteResponseDto
            {
                UserId = credential.UserId,
                Token = token,
                Success = true,
                Message = "Passkey login successful."
            };
        }


        // -------------------------------------------------------
        // PASSKEY RECOVERY
        // -------------------------------------------------------
        public async Task<PasskeyRecoveryBeginResponseDto> RecoveryBeginAsync(PasskeyRecoveryBeginRequestDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                throw new UserNotFoundException("Invalid recovery attempt.");

            var challengeId = Guid.NewGuid().ToString();

            _cache.Set(challengeId, new RecoverySession
            {
                Step = 1,
                UserId = user.Id,
                Email = user.Email
            }, _challengeTtl);

            return new PasskeyRecoveryBeginResponseDto
            {
                Success = true,
                ChallengeId = challengeId
            };
        }

        public async Task<PasskeyRecoveryVerifyCodeResponseDto> RecoveryVerifyCodeAsync(PasskeyRecoveryVerifyCodeRequestDto request)
        {
            if (!_cache.TryGetValue<RecoverySession>(request.ChallengeId, out var session) || session.Step != 1)
                throw new NotFoundException("Recovery session expired.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == session.UserId);

            if (user == null)
                throw new UserNotFoundException("User not found.");

            if (string.IsNullOrEmpty(user.RecoveryCodeHash) || 
                !RecoveryCodeHelper.VerifyRecoveryCode(request.RecoveryCode, user.RecoveryCodeHash))
                throw new InvalidCredentialsException("Invalid recovery code.");

            // Prepare FIDO2 options
            var fidoUser = new Fido2User
            {
                Id = Guid.NewGuid().ToByteArray(),
                Name = user.Email,
                DisplayName = user.Name
            };

            var options = _fido2.RequestNewCredential(fidoUser, new List<PublicKeyCredentialDescriptor>());

            var newChallengeId = Guid.NewGuid().ToString();

            _cache.Set(newChallengeId, new RecoverySession
            {
                Step = 2,
                UserId = user.Id,
                Email = user.Email,
                Options = options
            }, _challengeTtl);

            return new PasskeyRecoveryVerifyCodeResponseDto
            {
                ChallengeId = newChallengeId,
                Options = options
            };
        }

        public async Task<PasskeyRecoveryCompleteResponseDto> RecoveryCompleteAsync(PasskeyRecoveryCompleteRequestDto request)
        {
            if (!_cache.TryGetValue<RecoverySession>(request.ChallengeId, out var session) || session.Step != 2)
                throw new NotFoundException("Recovery session expired.");

            _cache.Remove(request.ChallengeId);

            var user = await _context.Users
                .Include(u => u.WebAuthnCredentials)
                .FirstOrDefaultAsync(u => u.Id == session.UserId);

            if (user == null)
                throw new UserNotFoundException("User not found.");

            var attestation = new AuthenticatorAttestationRawResponse
            {
                Id = Base64UrlHelper.Decode(request.RawId),
                RawId = Base64UrlHelper.Decode(request.RawId),
                Type = PublicKeyCredentialType.PublicKey,
                Response = new AuthenticatorAttestationRawResponse.ResponseData
                {
                    AttestationObject = Base64UrlHelper.Decode(request.Response.AttestationObject),
                    ClientDataJson = Base64UrlHelper.Decode(request.Response.ClientDataJSON)
                }
            };

            var makeResult = await _fido2.MakeNewCredentialAsync(
                attestation,
                session.Options!,
                async (args, ct) =>
                {
                    var id = Base64UrlHelper.Encode(args.CredentialId);
                    return !await _context.WebAuthnCredentials.AnyAsync(c => c.CredentialId == id);
                });

            if (makeResult?.Result == null)
                throw new Exception("Passkey creation failed.");

            // Remove old devices
            _context.WebAuthnCredentials.RemoveRange(user.WebAuthnCredentials);

            // Add new device
            var credential = new WebAuthnCredential
            {
                UserId = user.Id,
                CredentialId = Base64UrlHelper.Encode(makeResult.Result.CredentialId),
                PublicKey = Base64UrlHelper.Encode(makeResult.Result.PublicKey),
                SignCount = (int)makeResult.Result.Counter,
                CreatedAt = DateTime.UtcNow,
                DeviceName = string.IsNullOrWhiteSpace(request.DeviceName) ? null : request.DeviceName
            };

            _context.WebAuthnCredentials.Add(credential);

            // Generate new recovery key
            var newCode = RecoveryCodeHelper.GenerateRecoveryCode();
            user.RecoveryCodeHash = RecoveryCodeHelper.HashRecoveryCode(newCode);
            user.RecoveryCodeCreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new PasskeyRecoveryCompleteResponseDto
            {
                Success = true,
                Message = "Passkey successfully recovered.",
                NewRecoveryCode = newCode
            };
        }

        //---------------------------------------------------------------------------------------------------
        // MULTI-DEVICE MANAGEMENT
        //---------------------------------------------------------------------------------------------------
        // Get all devices for logged-in user
        public async Task<PasskeyDeviceListResponseDto> GetDevicesAsync(int userId)
        {
            var devices = await _context.WebAuthnCredentials
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new PasskeyDeviceDto
                {
                    CredentialId = c.CredentialId,
                    DeviceName = c.DeviceName,
                    CreatedAt = c.CreatedAt,
                    LastUsedAt = c.LastUsedAt
                }).ToListAsync();

            return new PasskeyDeviceListResponseDto
            {
                Devices = devices
            };
        }

        // Update device name
        public async Task<PasskeyDeviceDto> UpdateDeviceNameAsync(int userId, string credentialId, string deviceName)
        {
            var device = await _context.WebAuthnCredentials
                .FirstOrDefaultAsync(c => c.CredentialId == credentialId && c.UserId == userId);

            if (device == null) throw new NotFoundException("Device not found.");

            device.DeviceName = deviceName;
            await _context.SaveChangesAsync();

            return new PasskeyDeviceDto
            {
                CredentialId = device.CredentialId,
                DeviceName = device.DeviceName,
                CreatedAt = device.CreatedAt,
                LastUsedAt = device.LastUsedAt
            };
        }

        // Delete device
        public async Task DeleteDeviceAsync(int userId, string credentialId)
        {
            var device = await _context.WebAuthnCredentials
                .FirstOrDefaultAsync(c => c.CredentialId == credentialId && c.UserId == userId);

            if (device == null) throw new NotFoundException("Device not found.");

            _context.WebAuthnCredentials.Remove(device);
            await _context.SaveChangesAsync();
        }
        // -------------------------
        // Add Device (Begin)
        // -------------------------
        public async Task<PasskeyRegisterBeginResponseDto> AddDeviceBeginAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.WebAuthnCredentials)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) throw new UserNotFoundException("User not found.");

            var fidoUser = new Fido2User
            {
                Id = Encoding.UTF8.GetBytes(user.Id.ToString()),
                Name = user.Email,
                DisplayName = user.Name
            };

            var existingCreds = user.WebAuthnCredentials
                .Select(c => new PublicKeyCredentialDescriptor(Base64UrlHelper.Decode(c.CredentialId)))
                .ToList();

            var options = _fido2.RequestNewCredential(fidoUser, existingCreds);

            var challengeId = Guid.NewGuid().ToString();
            _cache.Set(challengeId, new RegistrationSession { Options = options }, _challengeTtl);

            return new PasskeyRegisterBeginResponseDto { Options = options, ChallengeId = challengeId };
        }

        // -------------------------
        // Add Device (Complete)
        // -------------------------
        public async Task<PasskeyRegisterCompleteResponseDto> AddDeviceCompleteAsync(int userId, PasskeyRegisterCompleteRequestDto request)
        {
            if (!_cache.TryGetValue<RegistrationSession>(request.ChallengeId, out var session))
                throw new NotFoundException("Passkey challenge expired.");

            _cache.Remove(request.ChallengeId);

            var attestationResponse = new AuthenticatorAttestationRawResponse
            {
                Id = Base64UrlHelper.Decode(request.RawId),
                RawId = Base64UrlHelper.Decode(request.RawId),
                Type = PublicKeyCredentialType.PublicKey,
                Response = new AuthenticatorAttestationRawResponse.ResponseData
                {
                    AttestationObject = Base64UrlHelper.Decode(request.Response.AttestationObject),
                    ClientDataJson = Base64UrlHelper.Decode(request.Response.ClientDataJSON)
                }
            };

            var makeResult = await _fido2.MakeNewCredentialAsync(
                attestationResponse,
                session.Options!,
                async (args, ct) =>
                {
                    var id = Base64UrlHelper.Encode(args.CredentialId);
                    return !await _context.WebAuthnCredentials.AnyAsync(c => c.CredentialId == id);
                });

            if (makeResult?.Result == null)
                throw new Exception("Attestation failed.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new UserNotFoundException("User not found.");

            var credential = new WebAuthnCredential
            {
                UserId = user.Id,
                CredentialId = Base64UrlHelper.Encode(makeResult.Result.CredentialId),
                PublicKey = Base64UrlHelper.Encode(makeResult.Result.PublicKey),
                SignCount = (int)makeResult.Result.Counter,
                CreatedAt = DateTime.UtcNow,
                DeviceName = string.IsNullOrWhiteSpace(request.DeviceName) ? null : request.DeviceName
            };

            _context.WebAuthnCredentials.Add(credential);
            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(user);

            return new PasskeyRegisterCompleteResponseDto
            {
                UserId = user.Id,
                CredentialId = credential.CredentialId,
                Token = token,
                Success = true,
                Message = "Device added successfully."
            };
        }


        //--------------------------------------------------------------------------------------------------
        // DELETE ACCOUNT
        //--------------------------------------------------------------------------------------------------
        public async Task DeleteAccountAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.WebAuthnCredentials)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new UserNotFoundException("User not found.");

            // Remove all passkeys
            _context.WebAuthnCredentials.RemoveRange(user.WebAuthnCredentials);

            // Remove user
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
        }

        // --------------------------------------------------------------------------------------------------
        // PROFILE
        // -----------------------------------------------------------------------------------------------------
        public async Task<UserProfileResponseDto> GetProfileAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.WebAuthnCredentials)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new UserNotFoundException("User not found.");

            return new UserProfileResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                HasPasskey = user.WebAuthnCredentials.Any()
            };
        }

    }
}







