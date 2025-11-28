namespace Backend.DTOs.Recovery
{
    public class PasskeyRecoveryBeginResponseDto
    {
        public string ChallengeId { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "Recovery started.";
    }

}