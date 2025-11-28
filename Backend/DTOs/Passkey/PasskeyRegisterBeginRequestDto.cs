namespace Backend.DTOs.Passkey
{
    public class PasskeyRegisterBeginRequestDto
    {
        //public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;  // Name of user
        public string Email { get; set; } = string.Empty; // Email of user
    }
}