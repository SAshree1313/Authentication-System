using Google.Apis.Auth;
using Backend.Exceptions;

namespace Backend.Helpers
{
    public static class GoogleTokenHelper
    {
        public static async Task<GoogleTokenResult> ValidateAsync(
            string idToken,
            string googleClientId)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new InvalidCredentialsException("Missing Google ID token.");

            GoogleJsonWebSignature.Payload payload;

            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { googleClientId }
                    });
            }
            catch
            {
                throw new InvalidCredentialsException("Invalid Google ID token.");
            }

            if (!payload.EmailVerified)
                throw new InvalidCredentialsException("Google email is not verified.");

            return new GoogleTokenResult
            {
                Sub = payload.Subject,
                Email = payload.Email,
                Name = payload.Name
            };
        }
    }
}
