using Absensi.Models;

namespace Absensi.Services
{
    /// <summary>CRUD user ber-role Anggota (delegasi ke RoleUserService).</summary>
    public class AnggotaService : RoleUserService
    {
        public AnggotaService(Database db, IPasswordService passwordService)
            : base(db, passwordService, RoleIds.Anggota)
        {
        }
    }
}