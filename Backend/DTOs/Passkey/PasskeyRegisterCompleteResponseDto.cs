namespace Backend.DTOs.Passkey
{
    public class PasskeyRegisterCompleteResponseDto
    {
        public int UserId { get; set; }
        public string CredentialId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;       // JWT token issued after registration
        public string RecoveryCode { get; set; } = string.Empty;   // Recovery code generated
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
