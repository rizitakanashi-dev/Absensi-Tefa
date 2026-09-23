using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    /// <summary>
    /// CRUD user generik berbasis role. Dipakai oleh AnggotaService,
    /// PMService, DevOpsService, dan GuruService (masing-masing tinggal
    /// menyuplai RoleIds). Semua method membatasi akses pada role tsb.
    /// </summary>
    public class RoleUserService
    {
        private readonly Database db;
        private readonly IPasswordService passwordService;
        private readonly int roleId;

        public RoleUserService(Database db, IPasswordService passwordService, int roleId)
        {
            this.db = db;
            this.passwordService = passwordService;
            this.roleId = roleId;
        }

        /// <summary>Daftar user dengan role ini.</summary>
        public virtual async Task<IEnumerable<UserDTO>> GetAll()
        {
            using var conn = db.connect();

            const string sql = @"
                SELECT u.id, u.nama, r.nama AS role, d.nama AS divisi
                FROM user u
                JOIN role r ON r.id = u.id_role
                LEFT JOIN divisi d ON d.id = u.id_divisi
                WHERE u.id_role = @RoleId
                ORDER BY u.id;";

            return await conn.QueryAsync<UserDTO>(sql, new { RoleId = roleId });
        }

        /// <summary>Detail user dengan role ini, atau null.</summary>
        public virtual async Task<UserDTO?> GetById(int id)
        {
            using var conn = db.connect();

            const string sql = @"
                SELECT u.id, u.nama, r.nama AS role, d.nama AS divisi
                FROM user u
                JOIN role r ON r.id = u.id_role
                LEFT JOIN divisi d ON d.id = u.id_divisi
                WHERE u.id = @Id AND u.id_role = @RoleId;";

            return await conn.QueryFirstOrDefaultAsync<UserDTO>(sql, new { Id = id, RoleId = roleId });
        }

        /// <summary>Daftarkan user baru dengan role ini.</summary>
        public virtual async Task<bool> Register(UserOTD data)
        {
            if (data is null)
                throw new ArgumentException("Data wajib diisi.");

            using var conn = db.connect();
            await conn.OpenAsync();
            await ValidateReferencesAsync(conn, data.id_divisi);

            string hashedPassword = passwordService.HashPassword(data.password);
            int? divisiId = data.id_divisi > 0 ? data.id_divisi : null;

            const string sql = @"
                INSERT INTO user(nama, password, id_role, id_divisi)
                VALUES(@nama, @password, @id_role, @id_divisi);";

            return await conn.ExecuteAsync(sql, new
            {
                nama = data.nama,
                password = hashedPassword,
                id_role = roleId,
                id_divisi = divisiId
            }) > 0;
        }

        /// <summary>
        /// Alias Register (dipakai GuruController). Disubclass boleh override.
        /// </summary>
        public virtual Task<bool> Create(UserOTD data) => Register(data);

        /// <summary>Perbarui nama & divisi user dengan role ini (Alias Create utk Guru).</summary>
        public virtual async Task<bool> Update(int id, UserOTD data)
        {
            if (data is null)
                throw new ArgumentException("Data wajib diisi.");

            using var conn = db.connect();
            await conn.OpenAsync();
            await ValidateReferencesAsync(conn, data.id_divisi);

            int? divisiId = data.id_divisi > 0 ? data.id_divisi : null;

            const string sql = @"
                UPDATE user
                SET nama = @nama,
                    id_divisi = @id_divisi
                WHERE id = @id AND id_role = @RoleId;";

            return await conn.ExecuteAsync(sql, new
            {
                id,
                nama = data.nama,
                id_divisi = divisiId,
                RoleId = roleId
            }) > 0;
        }

        /// <summary>Hapus user dengan role ini.</summary>
        public virtual async Task<bool> Delete(int id)
        {
            using var conn = db.connect();

            const string sql = "DELETE FROM user WHERE id = @id AND id_role = @RoleId;";
            return await conn.ExecuteAsync(sql, new { id, RoleId = roleId }) > 0;
        }

        private static async Task ValidateReferencesAsync(System.Data.Common.DbConnection conn, int? divisionId)
        {
            if (divisionId is > 0)
            {
                const string divisiSql = "SELECT COUNT(*) FROM divisi WHERE id = @Id;";
                if (await conn.ExecuteScalarAsync<int>(divisiSql, new { Id = divisionId }) == 0)
                    throw new ArgumentException("Divisi tidak ditemukan.");
            }
        }
    }
}