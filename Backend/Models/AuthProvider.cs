using System;

namespace Backend.Models
{
    public class AuthProvider
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string ProviderName { get; set; } = string.Empty;

        // Unique identifier from the provider for this user
        public string ProviderSub { get; set; } = string.Empty;

        // Store provider claims (id_token claims) as JSON text
        public string ProviderClaimsJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property to User
        public User? User { get; set; }
    }
}
