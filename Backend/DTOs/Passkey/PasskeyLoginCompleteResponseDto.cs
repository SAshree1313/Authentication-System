namespace Backend.DTOs.Passkey
{
    public class PasskeyLoginCompleteResponseDto
    {
        public int UserId { get; set; } 
        public string Token { get; set; } = string.Empty; // JWT token
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
