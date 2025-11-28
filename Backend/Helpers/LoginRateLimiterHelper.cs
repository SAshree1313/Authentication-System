using Microsoft.Extensions.Caching.Memory;
using Backend.Exceptions;

namespace Backend.Helpers
{
    public static class LoginRateLimiterHelper
    {
        public static bool IsCooldownActive(IMemoryCache cache, string email, out int secondsLeft)
        {
            var cooldownKey = $"login_cooldown:{email}";

            if (cache.TryGetValue<DateTime>(cooldownKey, out var cooldownUntil))
            {
                var now = DateTime.UtcNow;
                if (cooldownUntil > now)
                {
                    secondsLeft = (int)(cooldownUntil - now).TotalSeconds;
                    return true;
                }

                // Cooldown expired
                cache.Remove(cooldownKey);
            }

            secondsLeft = 0;
            return false;
        }

        public static void IncrementFailCount(IMemoryCache cache, string email)
        {
            var failKey = $"login_fail:{email}";
            var cooldownKey = $"login_cooldown:{email}";

            int fails = cache.TryGetValue<int>(failKey, out var count) ? count : 0;
            fails++;

            // Save count
            cache.Set(failKey, fails, TimeSpan.FromMinutes(10));

            if (fails >= 5)
            {
                int seconds = 300;
                var cooldownUntil = DateTime.UtcNow.AddSeconds(seconds);

                cache.Set(cooldownKey, cooldownUntil,
                    cooldownUntil - DateTime.UtcNow);

                cache.Remove(failKey);

                throw new ApiException($"COOLDOWN:{seconds}");
            }
        }

        public static void ResetFailCount(IMemoryCache cache, string email)
        {
            cache.Remove($"login_fail:{email}");
        }
    }
}
