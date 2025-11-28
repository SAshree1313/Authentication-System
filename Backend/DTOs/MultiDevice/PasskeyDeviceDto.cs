namespace Backend.DTOs.MultiDevice
{
    // Response for listing devices
    public class PasskeyDeviceDto
    {
        public string CredentialId { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }
}