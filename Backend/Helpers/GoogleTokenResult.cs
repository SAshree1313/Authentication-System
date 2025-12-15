namespace Backend.Helpers
{
    public class GoogleTokenResult
    {
        public string Sub { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }
}
