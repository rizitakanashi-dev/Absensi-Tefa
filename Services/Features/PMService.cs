using Absensi.Models;

namespace Absensi.Services
{
    /// <summary>CRUD user ber-role PM (delegasi ke RoleUserService).</summary>
    public class PMService : RoleUserService
    {
        public PMService(Database db, IPasswordService passwordService)
            : base(db, passwordService, RoleIds.PM)
        {
        }
    }
}