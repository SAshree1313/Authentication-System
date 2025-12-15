namespace Backend.DTOs.Auth
{
    public class GoogleLoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;

        // true = first time we've seen this user
        public bool IsFirstLogin { get; set; }

        // Recovery code for first-time users (null for existing users)
        public string? RecoveryCode { get; set; }
    }
}
