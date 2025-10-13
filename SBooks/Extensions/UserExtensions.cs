using System.Security.Claims;

namespace SBooks.Extensions
{
    public static class UserExtensions
    {
        public static long? GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst("UserId");
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }

        public static string? GetUserRole(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value;
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return user.IsInRole("Admin");
        }

        public static bool IsUser(this ClaimsPrincipal user)
        {
            return user.IsInRole("User");
        }
    }

    public static class DateTimeExtensions
    {
        public static DateTime ToLocalTime(this DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZoneInfo.Local);
        }

        public static string ToLocalTimeString(this DateTime? utcDateTime, string format = "dd/MM/yyyy HH:mm")
        {
            if (!utcDateTime.HasValue)
                return "";

            return utcDateTime.Value.ToLocalTime().ToString(format);
        }
    }
}