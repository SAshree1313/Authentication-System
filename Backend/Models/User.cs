using System;
using System.Collections.Generic;

namespace Backend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;       
        public string Email { get; set; } = string.Empty;

        // Account Level Email Verification Flag
        public bool EmailVerified { get; set; } = false;

         // Recovery system
        public string? RecoveryCodeHash { get; set; }
        public DateTime? RecoveryCodeCreatedAt { get; set; }
        public DateTime? RecoveryCodeUsedAt { get; set; }

        // Token versioning 
        // Starts at 1 and increments after sensitive actions
        public int TokenVersion { get; set; } = 1;


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for WebAuthn credentials and Auth Providers
        public List<WebAuthnCredential> WebAuthnCredentials { get; set; } = new List<WebAuthnCredential>();
        public List<AuthProvider> AuthProviders { get; set; } = new List<AuthProvider>();
    }
}
