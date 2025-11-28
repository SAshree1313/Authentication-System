namespace Backend.DTOs.Recovery
{
    public class PasskeyRecoveryVerifyCodeRequestDto
    {
        public string ChallengeId { get; set; } = string.Empty;
        public string RecoveryCode { get; set; } = string.Empty;
    }
}