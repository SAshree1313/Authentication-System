using System.Security.Claims;

namespace Backend.Helpers
{
    public static class JwtHelper
    {
        public static int? GetUserId(ClaimsPrincipal user)
        {
            var claim = user.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (claim == null)
                return null;

            return int.Parse(claim.Value);
        }
    }
}
