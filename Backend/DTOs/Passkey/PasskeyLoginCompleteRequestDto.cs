namespace Backend.DTOs.Passkey
{
    public class PasskeyLoginCompleteRequestDto
    {
        public string ChallengeId { get; set; }

        // Data from navigator.credentials.get()
        public string Id { get; set; }
        public string RawId { get; set; }
        public AssertionResponseDto Response { get; set; }
        public string Type { get; set; }
    }

    public class AssertionResponseDto
    {
        public string AuthenticatorData { get; set; }
        public string ClientDataJSON { get; set; }
        public string Signature { get; set; }
        public string UserHandle { get; set; }  // Contains userId for resident keys
    }
}
