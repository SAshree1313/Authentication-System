using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Backend.Helpers
{
    public static class JwtHelper
    {
        public static int? GetUserId(ClaimsPrincipal user)
        {
            if (user == null) 
                return null;

            // The three possible places userId may be stored
            string[] keys = new[]
            {
                "id",                               // custom claim (your primary)
                JwtRegisteredClaimNames.Sub,        // standard subject claim
                ClaimTypes.NameIdentifier           // fallback used by some identity providers
            };

            foreach (var key in keys)
            {
                var value = user.Claims.FirstOrDefault(c => c.Type == key)?.Value;
                if (int.TryParse(value, out int id))
                    return id;
            }

            return null; // nothing usable found
        }
    }
}
