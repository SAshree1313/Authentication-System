namespace Backend.DTOs.Passkey
{
    public class UserProfileResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool HasPasskey { get; set; }
    }
}