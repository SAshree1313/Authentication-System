using Backend.Data;
using Backend.DTOs.Passkey;
using Backend.Exceptions;
using Backend.Helpers;
using Backend.Models;
using Backend.Services.Token;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services.Passkey
{
    public class PasskeyService : IPasskeyService
    {
        private readonly AppDbContext _context;
        private readonly Fido2 _fido2;
        private readonly IMemoryCache _cache;
        private readonly ITokenService _tokenService;

        public PasskeyService(AppDbContext context, Fido2 fido2, IMemoryCache cache, ITokenService tokenService)
        {
            _context = context;
            _fido2 = fido2;
            _cache = cache;
            _tokenService = tokenService;
        }

        // ---------------------------------------------------------
        // BEGIN PASSKEY REGISTRATION
        // ---------------------------------------------------------
        public async Task<PasskeyRegisterBeginResponseDto> RegisterBeginAsync(PasskeyRegisterBeginRequestDto request)
        {
            // 1️⃣ Validate user
            var user = await _context.Users
                .Include(u => u.WebAuthnCredentials)
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user == null)
                throw new UserNotFoundException($"User with ID {request.UserId} not found.");

            // 2️⃣ Convert user → Fido2User
            var fidoUser = new Fido2User
            {
                Id = BitConverter.GetBytes(user.Id),
                Name = user.Email,
                DisplayName = user.Name
            };

            // 3️⃣ Convert existing credentials to FIDO2 descriptors
            var existingCredentials = user.WebAuthnCredentials.Select(c =>
                new PublicKeyCredentialDescriptor(
                    Base64UrlHelper.Decode(c.CredentialId)
                )
            ).ToList();

            // 4️⃣ Create registration options
            CredentialCreateOptions options;
            try
            {
                options = _fido2.RequestNewCredential(fidoUser, existingCredentials);
            }
            catch (Exception ex)
            {
                throw new Exception($"FIDO2 failed to generate registration options: {ex.Message}");
            }

            // 5️⃣ Generate challengeId
            var challengeId = Guid.NewGuid().ToString();

            // 6️⃣ Store challenge (RAM, 5 min)
            _cache.Set(challengeId, options, TimeSpan.FromMinutes(5));

            // 7️⃣ Return to frontend
            return new PasskeyRegisterBeginResponseDto
            {
                Options = options,
                ChallengeId = challengeId
            };
        }

        // ---------------------------------------------------------
        // COMPLETE PASSKEY REGISTRATION
        // ---------------------------------------------------------
        public async Task<PasskeyRegisterCompleteResponseDto> RegisterCompleteAsync(PasskeyRegisterCompleteRequestDto request)
        {
            // 1️⃣ Validate challenge existence
            if (!_cache.TryGetValue<CredentialCreateOptions>(request.ChallengeId, out var options) || options == null)
                throw new NotFoundException("Passkey challenge expired or not found.");

            _cache.Remove(request.ChallengeId); // one-use

            // 2️⃣ Build FIDO2 attestation response
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

            // 3️⃣ Validate attestation
            Fido2.CredentialMakeResult result;
            try
            {
                result = await _fido2.MakeNewCredentialAsync(
                    attestationResponse, 
                    options ?? throw new InvalidOperationException("Options cannot be null"),
                    async (args, ct) =>
                    {
                        // Check if this credential already exists
                        var credentialIdString = Base64UrlHelper.Encode(args.CredentialId);
                        var exists = await _context.WebAuthnCredentials
                            .AnyAsync(c => c.CredentialId == credentialIdString);
                        return !exists; // Return true if credential is unique to this user
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to verify passkey attestation: {ex.Message}");
            }

            // 4️⃣ Extract data
            if (result?.Result == null)
                throw new Exception("Attestation result is null.");
                
            var credentialId = Base64UrlHelper.Encode(result.Result.CredentialId);
            var publicKey = Base64UrlHelper.Encode(result.Result.PublicKey);
            var signCount = (int)result.Result.Counter;

            // 5️⃣ Get userId from challenge
            if (options?.User?.Id == null || options.User.Id.Length < 4)
                throw new Exception("Invalid user ID in challenge options.");
                
            var userId = BitConverter.ToInt32(options.User.Id, 0);

            // 6️⃣ Save credential
            var credential = new WebAuthnCredential
            {
                UserId = userId,
                CredentialId = credentialId,
                PublicKey = publicKey,
                SignCount = signCount
            };

            _context.WebAuthnCredentials.Add(credential);
            await _context.SaveChangesAsync();

            // 7️⃣ Return result
            return new PasskeyRegisterCompleteResponseDto
            {
                UserId = userId,
                CredentialId = credentialId,
                Success = true,
                Message = "Passkey registered successfully."
            };
        }

        // ---------------------------------------------------------
        // BEGIN PASSKEY LOGIN
        // ---------------------------------------------------------
        public async Task<PasskeyLoginBeginResponseDto> LoginBeginAsync()
        {
            // 1️⃣ Generate assertion options for discoverable credentials
            AssertionOptions options;
            try
            {
                options = _fido2.GetAssertionOptions(
                    allowedCredentials: new List<PublicKeyCredentialDescriptor>(),
                    userVerification: UserVerificationRequirement.Required
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate login assertion options: {ex.Message}");
            }

            // 2️⃣ Save challenge in RAM
            var challengeId = Guid.NewGuid().ToString();
            _cache.Set(challengeId, options, TimeSpan.FromMinutes(5));

            // 3️⃣ Return to frontend
            return new PasskeyLoginBeginResponseDto
            {
                Options = options,
                ChallengeId = challengeId
            };
        }

        // ---------------------------------------------------------
        // COMPLETE PASSKEY LOGIN
        // ---------------------------------------------------------
        public async Task<PasskeyLoginCompleteResponseDto> LoginCompleteAsync(PasskeyLoginCompleteRequestDto request)
        {
            // 1️⃣ Validate challenge exists
            if (!_cache.TryGetValue<AssertionOptions>(request.ChallengeId, out var options))
                throw new NotFoundException("Passkey challenge expired or not found.");
            _cache.Remove(request.ChallengeId); // one-use

            // 2️⃣ Build FIDO2 assertion response
            var assertionResponse = new AuthenticatorAssertionRawResponse
            {
                Id = Base64UrlHelper.Decode(request.RawId),
                RawId = Base64UrlHelper.Decode(request.RawId),
                Type = PublicKeyCredentialType.PublicKey
            };
            
            // Initialize response if null, then set properties
            if (assertionResponse.Response == null)
            {
                assertionResponse.Response = new AuthenticatorAssertionRawResponse.AssertionResponse();
            }
            
            assertionResponse.Response.AuthenticatorData = Base64UrlHelper.Decode(request.Response.AuthenticatorData);
            assertionResponse.Response.ClientDataJson = Base64UrlHelper.Decode(request.Response.ClientDataJSON);
            assertionResponse.Response.Signature = Base64UrlHelper.Decode(request.Response.Signature);
            assertionResponse.Response.UserHandle = !string.IsNullOrEmpty(request.Response.UserHandle)
                ? Base64UrlHelper.Decode(request.Response.UserHandle)
                : null;

            // 3️⃣ Validate assertion with FIDO2
            WebAuthnCredential? credentialEntity;
            AssertionVerificationResult result;
            try
            {
                var credentialIdString = Base64UrlHelper.Encode(assertionResponse.Id);
                credentialEntity = await _context.WebAuthnCredentials
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CredentialId == credentialIdString);

                if (credentialEntity == null)
                    throw new InvalidCredentialsException("Assertion failed: unknown credential.");

                var publicKeyBytes = Base64UrlHelper.Decode(credentialEntity.PublicKey);
                var storedCounter = (uint)credentialEntity.SignCount;

                result = await _fido2.MakeAssertionAsync(
                    assertionResponse,
                    options ?? throw new InvalidOperationException("Options cannot be null"),
                    publicKeyBytes,
                    storedCounter,
                    (userHandleOwner, credentialId) =>
                    {
                        return Task.FromResult(credentialEntity.UserId > 0);
                    });

                if (result.CredentialId == null)
                    throw new InvalidCredentialsException("Assertion failed: unknown credential.");
            }
            catch (InvalidCredentialsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to verify passkey assertion: {ex.Message}");
            }

            // 4️⃣ Update sign count safely
            try
            {
                if (credentialEntity != null)
                {
                    credentialEntity.SignCount = (int)result.Counter;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update credential sign count: {ex.Message}");
            }

            // 5️⃣ Generate JWT token
            string jwt;
            try
            {
                if (credentialEntity?.User == null)
                    throw new InvalidOperationException("User not found for credential.");
                    
                jwt = _tokenService.GenerateToken(credentialEntity.User);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate JWT token: {ex.Message}");
            }

            // 6️⃣ Return result
            return new PasskeyLoginCompleteResponseDto
            {
                UserId = credentialEntity.UserId,
                Token = jwt,
                Success = true,
                Message = "Passkey login successful."
            };
        }
    }
}
