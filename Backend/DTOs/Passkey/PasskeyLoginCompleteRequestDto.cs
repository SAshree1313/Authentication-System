namespace Backend.DTOs.Passkey
{
    public class PasskeyLoginCompleteRequestDto
    {
        public string ChallengeId { get; set; } = string.Empty;

        // Data from navigator.credentials.get()
        public string Id { get; set; } = string.Empty;
        public string RawId { get; set; } = string.Empty;
        public AssertionResponseDto Response { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public class AssertionResponseDto
    {
        public string AuthenticatorData { get; set; } = string.Empty;
        public string ClientDataJSON { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string? UserHandle { get; set; }  // Contains userId for resident keys
    }
}
