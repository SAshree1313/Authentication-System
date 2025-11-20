namespace Backend.DTOs.Passkey
{
    public class UserProfileResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool HasPasskey { get; set; }
    }
}
