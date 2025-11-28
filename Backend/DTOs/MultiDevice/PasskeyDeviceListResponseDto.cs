namespace Backend.DTOs.MultiDevice
{
    public class PasskeyDeviceListResponseDto
    {
        public List<PasskeyDeviceDto> Devices { get; set; } = new();
        public bool Success { get; set; } = true;
    }

}