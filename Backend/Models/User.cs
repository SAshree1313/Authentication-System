using System;
using System.Collections.Generic;

namespace Backend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;       // Added Name
        public string Email { get; set; } = string.Empty;

        // Password not used in passkey-only auth
        //public string PasswordHash { get; set; }

         // Recovery system
        public string? RecoveryCodeHash { get; set; }
        public DateTime? RecoveryCodeCreatedAt { get; set; }
        public DateTime? RecoveryCodeUsedAt { get; set; }

        // Token versioning (NEW)
        // Starts at 1 and increments after sensitive actions
        public int TokenVersion { get; set; } = 1;


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for WebAuthn credentials
        public List<WebAuthnCredential> WebAuthnCredentials { get; set; } = new List<WebAuthnCredential>();
    }
}
