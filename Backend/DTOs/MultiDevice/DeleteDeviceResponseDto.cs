namespace Backend.DTOs.MultiDevice
{
    public class DeleteDeviceResponseDto
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = "Device deleted successfully.";
    }
}
