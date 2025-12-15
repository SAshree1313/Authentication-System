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

        // Small internal session carriers stored in IMemoryCache during WebAuthn flows
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
        // PASSKEY REGISTRATION (BEGIN)
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

            // Cache the session for the short time the browser will perform WebAuthn
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

        // -------------------------------------------------------
        // PASSKEY REGISTRATION (COMPLETE) - ATOMIC
        // - Create User + Credential in a DB transaction so no partial user exists
        // -------------------------------------------------------
        public async Task<PasskeyRegisterCompleteResponseDto> RegisterCompleteAsync(PasskeyRegisterCompleteRequestDto request)
        {
            if (!_cache.TryGetValue<RegistrationSession>(request.ChallengeId, out var session))
                throw new NotFoundException("Passkey challenge expired.");

            // Prevent replay by removing the challenge immediately
            _cache.Remove(request.ChallengeId);

            // Build attestation object required by FIDO2 library
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

            // Validate attestation and ensure credential id is unused
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

            // Final check: ensure email wasn't registered while browser performed WebAuthn
            if (await _context.Users.AnyAsync(u => u.Email == session.Email))
                throw new DuplicateEmailException("Email already exists.");

            // Use a DB transaction to guarantee atomic user + credential creation
            await using var txn = await _context.Database.BeginTransactionAsync();
            try
            {
                var recoveryCode = RecoveryCodeHelper.GenerateRecoveryCode();

                // Create user entity
                var user = new User
                {
                    Name = session.Name,
                    Email = session.Email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    RecoveryCodeHash = RecoveryCodeHelper.HashRecoveryCode(recoveryCode),
                    RecoveryCodeCreatedAt = DateTime.UtcNow,
                    TokenVersion = 1
                };

                _context.Users.Add(user);
                // Save to get user.Id assigned (tracked by EF)
                await _context.SaveChangesAsync();

                // Create credential entity referencing the newly created user
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

                // Commit the transaction only after both user and credential are persisted
                await txn.CommitAsync();

                // Generate JWT for the new user (safe: user.TokenVersion already set)
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
                // Rollback on any error to prevent partial writes
                try { await txn.RollbackAsync(); } catch { /* swallow rollback errors */ }
                throw;
            }
        }

        // -------------------------------------------------------
        // PASSKEY LOGIN (BEGIN)
        // -------------------------------------------------------
        public async Task<PasskeyLoginBeginResponseDto> LoginBeginAsync(PasskeyLoginBeginRequestDto request)
        {
            var validation = await _loginValidator.ValidateAsync(request);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);

            if (LoginRateLimiterHelper.IsCooldownActive(_cache, request.Email, out var secs))
                throw new ApiException($"COOLDOWN:{secs}");

            var user = await _context.Users
                .Include(u => u.WebAuthnCredentials)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
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

        // -------------------------------------------------------
        // PASSKEY LOGIN (COMPLETE)
        // -------------------------------------------------------
        public async Task<PasskeyLoginCompleteResponseDto> LoginCompleteAsync(PasskeyLoginCompleteRequestDto request)
        {
            if (!_cache.TryGetValue<LoginSession>(request.ChallengeId, out var session))
                throw new NotFoundException("Login session expired.");

            // Prevent replay
            _cache.Remove(request.ChallengeId);

            var credentialIdString = Base64UrlHelper.Encode(Base64UrlHelper.Decode(request.RawId));

            var credential = await _context.WebAuthnCredentials
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CredentialId == credentialIdString);

            if (credential == null || credential.UserId != session.UserId)
            {
                var email = await _context.Users.Where(u => u.Id == session.UserId).Select(u => u.Email).FirstOrDefaultAsync();
                if (email != null)
                    LoginRateLimiterHelper.IncrementFailCount(_cache, email);

                throw new InvalidCredentialsException("Invalid credentials.");
            }

            // Build assertion for FIDO2 library
            var assertion = new AuthenticatorAssertionRawResponse
            {
                Id = Base64UrlHelper.Decode(request.RawId),
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
                }
            };

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

            // Reset rate limiter on success
            LoginRateLimiterHelper.ResetFailCount(_cache, credential.User!.Email);

            // Update sign count and last used timestamp and persist
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

            // Mark the recovery code as used for auditing
            user.RecoveryCodeUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Prepare FIDO2 options
            var fidoUser = new Fido2User
            {
                Id = Encoding.UTF8.GetBytes(user.Id.ToString()),
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

            Console.WriteLine($"[Recovery] Starting recovery for UserId: {session.UserId}");

            // Load user
            var user = await _context.Users
                .Include(u => u.WebAuthnCredentials)
                .Include(u => u.AuthProviders)
                .FirstOrDefaultAsync(u => u.Id == session.UserId);

            if (user == null)
                throw new UserNotFoundException("User not found.");

            Console.WriteLine($"[Recovery] User found: {user.Email}, Current credentials count: {user.WebAuthnCredentials.Count}");

            // Build attestation
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

            // Validate attestation
            var makeResult = await _fido2.MakeNewCredentialAsync(
                attestation,
                session.Options!,
                async (args, ct) =>
                {
                    var id = Base64UrlHelper.Encode(args.CredentialId);
                    return !await _context.WebAuthnCredentials.AnyAsync(c => c.CredentialId == id);
                });

            if (makeResult?.Result == null)
            {
                Console.WriteLine("FIDO2 Recovery Error: " + makeResult.Status + " | " + makeResult.ErrorMessage);
                throw new Exception("Passkey creation failed: " + makeResult.ErrorMessage);
            }

            Console.WriteLine("[Recovery] FIDO2 validation successful");

            await using var txn = await _context.Database.BeginTransactionAsync();

            try
            {
                // Delete all old credentials FIRST
                var oldCreds = user.WebAuthnCredentials.ToList();
                
                if (oldCreds.Any())
                {
                    _context.WebAuthnCredentials.RemoveRange(oldCreds);
                    await _context.SaveChangesAsync();
                }

                if (user.AuthProviders.Any())
                {
                    Console.WriteLine($"[Recovery] Removing {user.AuthProviders.Count} auth providers");
                    _context.AuthProviders.RemoveRange(user.AuthProviders);
                    await _context.SaveChangesAsync();
                }

                // Now insert the new credential
                var newCred = new WebAuthnCredential
                {
                    UserId = user.Id,
                    CredentialId = Base64UrlHelper.Encode(makeResult.Result.CredentialId),
                    PublicKey = Base64UrlHelper.Encode(makeResult.Result.PublicKey),
                    SignCount = (int)makeResult.Result.Counter,
                    CreatedAt = DateTime.UtcNow,
                    DeviceName = string.IsNullOrWhiteSpace(request.DeviceName) ? null : request.DeviceName
                };

                Console.WriteLine($"[Recovery] Adding new credential: {newCred.CredentialId}, DeviceName: {newCred.DeviceName ?? "null"}");

                _context.WebAuthnCredentials.Add(newCred);
                
                // New recovery code + token version bump
                var newCode = RecoveryCodeHelper.GenerateRecoveryCode();
                user.RecoveryCodeHash = RecoveryCodeHelper.HashRecoveryCode(newCode);
                user.RecoveryCodeCreatedAt = DateTime.UtcNow;
                user.RecoveryCodeUsedAt = null;   // <-- reset because this applies to old code
                user.TokenVersion += 1;

                await _context.SaveChangesAsync();
                
                Console.WriteLine($"[Recovery] New credential saved with ID: {newCred.Id}, User TokenVersion: {user.TokenVersion}");
                
                Console.WriteLine($"[Recovery] User updated, new TokenVersion: {user.TokenVersion}");

                await txn.CommitAsync();

                Console.WriteLine("[Recovery] Transaction committed successfully");

                // Verify the credential was saved
                var savedCred = await _context.WebAuthnCredentials
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CredentialId == newCred.CredentialId);
                
                Console.WriteLine($"[Recovery] Verification - Credential exists in DB: {savedCred != null}, ID: {savedCred?.Id}, DeviceName: {savedCred?.DeviceName ?? "null"}");

                return new PasskeyRecoveryCompleteResponseDto
                {
                    Success = true,
                    Message = "Passkey successfully recovered.",
                    NewRecoveryCode = newCode
                };
            }
            catch (Exception ex)
            {
                await txn.RollbackAsync();
                Console.WriteLine($"[Recovery] Transaction rolled back due to error: {ex.Message}");
                Console.WriteLine($"[Recovery] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // -------------------------------------------------------
        // MULTI-DEVICE MANAGEMENT
        // -------------------------------------------------------
        // Get devices (read-only)
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

        // Update device name (single field change - atomicity not strictly required but we still persist safely)
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

        // Delete device - ATOMIC (remove credential + bump token version)
        public async Task<DeleteDeviceResponseDto> DeleteDeviceAsync(int userId, string credentialId)
        {
            await using var txn = await _context.Database.BeginTransactionAsync();
            try
            {
                var device = await _context.WebAuthnCredentials
                    .FirstOrDefaultAsync(c => c.CredentialId == credentialId && c.UserId == userId);

                if (device == null)
                {
                    await txn.RollbackAsync();
                    throw new NotFoundException("Device not found.");
                }

                _context.WebAuthnCredentials.Remove(device);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    await txn.RollbackAsync();
                    throw new UserNotFoundException("User not found.");
                }

                // TokenVersion bump invalidates old JWTs
                user.TokenVersion += 1;

                await _context.SaveChangesAsync();
                await txn.CommitAsync();

                // Generate updated JWT
                var newToken = _tokenService.GenerateToken(user);

                return new DeleteDeviceResponseDto
                {
                    Success = true,
                    Token = newToken,
                    Message = "Device deleted successfully."
                };
            }
            catch
            {
                try { await txn.RollbackAsync(); } catch { }
                throw;
            }
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
        // Add Device (Complete) - ATOMIC
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

            // Start transaction to ensure credential addition + tokenVersion bump are atomic
            await using var txn = await _context.Database.BeginTransactionAsync();
            try
            {
                // Re-load user with tracking inside transaction
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    await txn.RollbackAsync();
                    throw new UserNotFoundException("User not found.");
                }

                // Create credential
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

                // Invalidate old tokens
                user.TokenVersion += 1;

                await _context.SaveChangesAsync();
                await txn.CommitAsync();

                // Generate a fresh token for the user (reflects new TokenVersion)
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
            catch
            {
                try { await txn.RollbackAsync(); } catch { /* swallow */ }
                throw;
            }
        }

        //--------------------------------------------------------------------------------------------------
        // DELETE ACCOUNT - ATOMIC
        // Remove passkeys and user in a single transaction to avoid orphans
        //--------------------------------------------------------------------------------------------------
        public async Task DeleteAccountAsync(int userId)
        {
            await using var txn = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .Include(u => u.WebAuthnCredentials)
                    .Include(u => u.AuthProviders)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    await txn.RollbackAsync();
                    throw new UserNotFoundException("User not found.");
                }

                // Remove credentials and user in same transaction
                _context.WebAuthnCredentials.RemoveRange(user.WebAuthnCredentials);
                _context.AuthProviders.RemoveRange(user.AuthProviders);
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();
                await txn.CommitAsync();
            }
            catch
            {
                try { await txn.RollbackAsync(); } catch { /* swallow */ }
                throw;
            }
        }

        // --------------------------------------------------------------------------------------------------
        // PROFILE (read-only)
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