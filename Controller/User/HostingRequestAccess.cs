using System.Security.Claims;
using Absensi.Models;
using Absensi.Services;

namespace Absensi.Controller
{
    /// <summary>
    /// Ringkasan identitas caller yang diekstrak dari JWT, dipakai handler
    /// hosting agar tidak mengulang pola TryGetUserId + GetRoleName + IsInRole
    /// di setiap endpoint.
    /// </summary>
    public readonly record struct CallerContext(int UserId, string Role, bool IsAdmin);

    public static class HostingRequestAccess
    {
        /// <summary>
        /// Bangun CallerContext dari token. Return false bila userId tidak ada.
        /// </summary>
        public static bool TryResolveCaller(this ClaimsPrincipal user, out CallerContext caller)
        {
            caller = default;
            if (!user.TryGetUserId(out var userId))
                return false;

            var role = user.GetRoleName() ?? string.Empty;
            caller = new CallerContext(userId, role, user.IsInRole("Admin"));
            return true;
        }

        /// <summary>
        /// Apakah caller boleh melihat/mengedit request tertentu?
        /// Staff (Admin, PM, DevOps) melihat semua; Anggota hanya miliknya.
        /// Role lain (mis. Guru) tidak punya akses.
        /// </summary>
        public static bool CanAccessRequest(this CallerContext caller, HostingRequestDTO request)
        {
            if (caller.Role is "Admin" or "PM" or "DevOps")
                return true;

            return caller.Role == "Anggota" && request.IdUser == caller.UserId;
        }

        /// <summary>Apakah caller termasuk role yang melihat semua request?</summary>
        public static bool IsStaff(this CallerContext caller)
            => caller.Role is "Admin" or "PM" or "DevOps";
    }
}