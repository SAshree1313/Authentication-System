namespace Backend.DTOs.Recovery
{
    public class PasskeyRecoveryCompleteRequestDto
    {
        public string ChallengeId { get; set; } = string.Empty;
        public string RawId { get; set; } = string.Empty;
        public PasskeyAttestationResponseDto Response { get; set; } = null!;
        public string DeviceName { get; set; } = string.Empty;
    }

    public class PasskeyAttestationResponseDto
    {
        public string AttestationObject { get; set; } = string.Empty;
        public string ClientDataJSON { get; set; } = string.Empty;
    }
}
