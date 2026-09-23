using Absensi.Models;

namespace Absensi.Services
{
    /// <summary>CRUD user ber-role DevOps (delegasi ke RoleUserService).</summary>
    public class DevOpsService : RoleUserService
    {
        public DevOpsService(Database db, IPasswordService passwordService)
            : base(db, passwordService, RoleIds.DevOps)
        {
        }
    }
}