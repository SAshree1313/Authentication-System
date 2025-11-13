using System;

namespace Backend.Models
{
    public class WebAuthnCredential
    {
        public int Id { get; set; }
        public int UserId { get; set; }               // Foreign key
        public string CredentialId { get; set; }      // Unique
        public string PublicKey { get; set; }
        public int SignCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property to User
        public User User { get; set; }
    }
}