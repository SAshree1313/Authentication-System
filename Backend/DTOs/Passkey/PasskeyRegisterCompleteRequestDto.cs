namespace Backend.DTOs.Passkey
{
    public class PasskeyRegisterCompleteRequestDto
    {
        public string ChallengeId { get; set; }

        // Raw data returned by navigator.credentials.create()
        public string Id { get; set; }
        public string RawId { get; set; }
        public AttestationResponseDto Response { get; set; }
        public string Type { get; set; }
    }

    public class AttestationResponseDto
    {
        public string ClientDataJSON { get; set; }
        public string AttestationObject { get; set; }
    }
}
