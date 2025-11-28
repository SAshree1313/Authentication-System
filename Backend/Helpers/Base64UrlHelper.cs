using System;

namespace Backend.Helpers
{
    public static class Base64UrlHelper
    {
        public static byte[] Decode(string base64Url)
        {
            if (string.IsNullOrWhiteSpace(base64Url))
                throw new ArgumentException("Input cannot be null or empty.", nameof(base64Url));

            string padded = base64Url
                .Replace('-', '+')
                .Replace('_', '/');

            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }   

            return Convert.FromBase64String(padded);
        }

        public static string Encode(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Input cannot be null or empty.", nameof(data));

            return Convert.ToBase64String(data)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
