using Absensi.Models;

namespace Absensi.Services
{
    /// <summary>CRUD user ber-role Guru (delegasi ke RoleUserService).</summary>
    public class GuruService : RoleUserService
    {
        public GuruService(Database db, IPasswordService passwordService)
            : base(db, passwordService, RoleIds.Guru)
        {
        }
    }
}