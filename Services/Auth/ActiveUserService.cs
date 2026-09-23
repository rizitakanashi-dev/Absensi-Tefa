using Dapper;

namespace Absensi.Services
{
    /// <summary>
    /// Validasi status user aktif terhadap database. Dipakai oleh
    /// UserStateValidationMiddleware agar JWT stateless tidak menjadi
    /// satu-satunya sumber kebenaran (token tetap berlaku 7 hari meski
    /// akun sudah dihapus / role diturunkan).
    /// </summary>
    public class ActiveUserService
    {
        private readonly Database db;

        public ActiveUserService(Database db)
        {
            this.db = db;
        }

        /// <summary>
        /// Mengembalikan true jika user dengan id tersebut masih ada di
        /// tabel user dan role DB-nya masih sama dengan claim token.
        /// </summary>
        public async Task<bool> IsStillActiveAsync(int userId, string? tokenRole, CancellationToken ct = default)
        {
            using var conn = db.connect();
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT r.nama
                FROM user u
                INNER JOIN role r ON r.id = u.id_role
                WHERE u.id = @Id;";

            var dbRole = await conn.QueryFirstOrDefaultAsync<string?>(sql, new { Id = userId });

            // User tidak ditemukan (dihapus) atau role DB berubah (diturunkan) = tidak aktif.
            if (dbRole is null)
                return false;

            return string.IsNullOrEmpty(tokenRole) || string.Equals(dbRole, tokenRole, StringComparison.OrdinalIgnoreCase);
        }
    }
}