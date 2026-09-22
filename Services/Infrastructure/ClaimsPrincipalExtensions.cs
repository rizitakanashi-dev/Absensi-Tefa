using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Absensi.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool TryGetUserId(this ClaimsPrincipal user, out int userId)
        {
            var raw =
                user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirstValue("sub");

            return int.TryParse(raw, out userId) && userId > 0;
        }

        public static string? GetRoleName(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Role)
                ?? user.FindFirstValue("role")
                ?? user.Claims.FirstOrDefault(c =>
                    c.Type.EndsWith("/identity/claims/role", StringComparison.OrdinalIgnoreCase))?.Value;
        }
    }
}
