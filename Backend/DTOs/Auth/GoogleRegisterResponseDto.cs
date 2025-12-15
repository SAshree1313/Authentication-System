namespace Backend.DTOs.Auth
{
    public class GoogleRegisterResponseDto
    {
        public bool IsNewUser { get; set; }

        public string AccessToken { get; set; } = string.Empty;

        // Returned ONLY for brand-new users
        // Null when existing passkey user is linked
        public string? RecoveryCode { get; set; }
    }
}
