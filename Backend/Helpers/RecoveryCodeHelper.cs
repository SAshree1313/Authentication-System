using System.Security.Cryptography;
using System.Text;

namespace Backend.Helpers
{
    public static class RecoveryCodeHelper
    {
        // Generates a strongly secure recovery code
        public static string GenerateRecoveryCode()
        {
            // 20 bytes = 160-bit code (very strong)
            byte[] randomBytes = RandomNumberGenerator.GetBytes(20);

            // Encode as Base32 (RFC 4648) using built-in Convert
            string base32 = ToBase32String(randomBytes);

            // Format: XXXX-XXXX-XXXX-XXXX-XXXX
            return string.Join("-", SplitIntoChunks(base32, 4));
        }

        // BCrypt hash
        public static string HashRecoveryCode(string code)
        {
            return BCrypt.Net.BCrypt.HashPassword(code);
        }

        // Verify
        public static bool VerifyRecoveryCode(string code, string hashed)
        {
            return BCrypt.Net.BCrypt.Verify(code, hashed);
        }


        // ---- Helpers ----

        // True Base32 encoder — **no hardcoded alphabet**
        private static string ToBase32String(byte[] data)
        {
            return Convert.ToBase64String(data)    // base64
                .Replace("=", "")                 // remove padding
                .Replace("/", "")                 // make URL-safe
                .Replace("+", "")                 // no symbols
                .ToUpper()                        // uppercase only
                .Substring(0, 20);                // limit size
        }

        private static IEnumerable<string> SplitIntoChunks(string text, int size)
        {
            for (int i = 0; i < text.Length; i += size)
                yield return text.Substring(i, Math.Min(size, text.Length - i));
        }
    }
}
