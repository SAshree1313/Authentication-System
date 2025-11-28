namespace Backend.DTOs.Passkey
{
    public class PasskeyRegisterCompleteRequestDto
    {
        public string ChallengeId { get; set; } = string.Empty; // Used to retrieve challenge from server cache

        // Raw data returned by navigator.credentials.create()
        public string Id { get; set; } = string.Empty;
        public string RawId { get; set; } = string.Empty;
        public AttestationResponseDto Response { get; set; } = new AttestationResponseDto();
        public string Type { get; set; } = string.Empty;
        public string? DeviceName { get; set; } = null;
    }

    public class AttestationResponseDto
    {
        public string ClientDataJSON { get; set; } = string.Empty;
        public string AttestationObject { get; set; } = string.Empty;
    }
}
