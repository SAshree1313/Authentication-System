using Fido2NetLib;

namespace Backend.DTOs.Passkey
{
    public class PasskeyRegisterBeginResponseDto
    {
        public CredentialCreateOptions? Options { get; set; }
        public string? ChallengeId { get; set; }   // used for linking the challenge in cache
    }
}
