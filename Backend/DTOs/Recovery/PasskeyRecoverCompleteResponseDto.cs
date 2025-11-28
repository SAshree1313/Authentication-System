namespace Backend.DTOs.Recovery
{
    public class PasskeyRecoveryCompleteResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NewRecoveryCode { get; set; } = string.Empty;
    }

}
