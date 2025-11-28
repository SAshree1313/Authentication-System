using System;

namespace Backend.Models
{
    public class WebAuthnCredential
    {
        public int Id { get; set; }
        public int UserId { get; set; }               // Foreign key
        public string CredentialId { get; set; } = string.Empty;      // Unique
        public string PublicKey { get; set; } = string.Empty;
        public int SignCount { get; set; } = 0;

        public string? DeviceName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }


        // Navigation property to User
        public User? User { get; set; }
    }
}