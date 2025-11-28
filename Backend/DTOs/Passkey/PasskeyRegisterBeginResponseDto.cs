using Fido2NetLib;

namespace Backend.DTOs.Passkey
{
    public class PasskeyRegisterBeginResponseDto
    {
        public CredentialCreateOptions? Options { get; set; } // FIDO2 options for navigator.credentials.create()
        public string ChallengeId { get; set; } = string.Empty; // Used to store/retrieve challenge in server cache
    }
}
