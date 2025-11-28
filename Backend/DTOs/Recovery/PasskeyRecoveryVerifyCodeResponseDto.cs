using Fido2NetLib;
namespace Backend.DTOs.Recovery
{
    public class PasskeyRecoveryVerifyCodeResponseDto
    {
        public string ChallengeId { get; set; } = string.Empty;
        public CredentialCreateOptions Options { get; set; } = null!;
    }

}
