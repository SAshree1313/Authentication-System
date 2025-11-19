namespace Backend.DTOs.Passkey
{
    public class PasskeyRegisterCompleteResponseDto
    {
        public int UserId { get; set; }
        public string CredentialId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
