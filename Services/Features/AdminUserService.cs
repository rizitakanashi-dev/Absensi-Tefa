using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    public class AdminUserService
    {
        private readonly Database db;
        private readonly IPasswordService passwordService;

        public AdminUserService(Database db, IPasswordService passwordService)
        {
            this.db = db;
            this.passwordService = passwordService;
        }

        public async Task<IEnumerable<UserDTO>> GetAll(int? roleId = null)
        {
            using var conn = db.connect();
            const string sql = @"
                SELECT u.id, u.nama, r.nama AS Role, d.nama AS Divisi
                FROM user u
                INNER JOIN role r ON r.id = u.id_role
                LEFT JOIN divisi d ON d.id = u.id_divisi
                WHERE (@RoleId IS NULL OR u.id_role = @RoleId)
                ORDER BY u.id;";

            return await conn.QueryAsync<UserDTO>(sql, new { RoleId = roleId });
        }

        public async Task<UserDTO?> GetById(int id)
        {
            using var conn = db.connect();
            const string sql = @"
                SELECT u.id, u.nama, r.nama AS Role, d.nama AS Divisi
                FROM user u
                INNER JOIN role r ON r.id = u.id_role
                LEFT JOIN divisi d ON d.id = u.id_divisi
                WHERE u.id = @Id;";

            return await conn.QueryFirstOrDefaultAsync<UserDTO>(sql, new { Id = id });
        }

        public async Task<int> Create(AdminUserCreateDTO data)
        {
            ValidateName(data.Nama);
            ValidatePassword(data.Password);
            ValidateRole(data.IdRole);

            using var conn = db.connect();
            await conn.OpenAsync();
            await ValidateReferences(conn, data.IdRole, data.IdDivisi);
            await EnsureNameAvailable(conn, data.Nama.Trim());

            const string sql = @"
                INSERT INTO user (nama, password, id_role, id_divisi)

                VALUES (@Nama, @Password, @IdRole, @IdDivisi);
                SELECT LAST_INSERT_ID();";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                Nama = data.Nama.Trim(),
                Password = passwordService.HashPassword(data.Password),
                data.IdRole,
                IdDivisi = NormalizeDivision(data.IdDivisi)
            });
        }

        public async Task<bool> Update(int actorId, int id, AdminUserUpdateDTO data)
        {
            ValidateName(data.Nama);
            ValidateRole(data.IdRole);

            using var conn = db.connect();
            await conn.OpenAsync();
            await ValidateReferences(conn, data.IdRole, data.IdDivisi);
            await EnsureNameAvailable(conn, data.Nama.Trim(), id);

            if (actorId == id && data.IdRole != RoleIds.Admin)
                throw new InvalidOperationException("Admin tidak dapat menurunkan role dirinya sendiri.");

            var parameters = new DynamicParameters();
            parameters.Add("Id", id);
            parameters.Add("Nama", data.Nama.Trim());
            parameters.Add("IdRole", data.IdRole);
            parameters.Add("IdDivisi", NormalizeDivision(data.IdDivisi));

            var passwordClause = string.Empty;
            if (!string.IsNullOrWhiteSpace(data.Password))
            {
                ValidatePassword(data.Password);
                passwordClause = ", password = @Password";
                parameters.Add("Password", passwordService.HashPassword(data.Password));
            }

            const string sql = @"
                UPDATE user
                SET nama = @Nama,
                    id_role = @IdRole,
                    id_divisi = @IdDivisi,
                    refresh_token = NULL,
                    refresh_token_expired = NULL" + "{PASSWORD_CLAUSE}" + @"
                WHERE id = @Id;";

            return await conn.ExecuteAsync(sql.Replace("{PASSWORD_CLAUSE}", passwordClause), parameters) > 0;
        }

        public async Task<bool> Delete(int actorId, int id)
        {
            if (actorId == id)
                throw new InvalidOperationException("Admin tidak dapat menghapus akun sendiri.");

            using var conn = db.connect();
            await conn.OpenAsync();
            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                const string targetSql = "SELECT id_role FROM user WHERE id = @Id FOR UPDATE;";
                var targetRole = await conn.ExecuteScalarAsync<int?>(targetSql, new { Id = id }, transaction);
                if (!targetRole.HasValue)
                    return false;

                if (targetRole.Value == RoleIds.Admin)
                {
                    const string adminCountSql = "SELECT COUNT(*) FROM user WHERE id_role = @AdminRole;";
                    var adminCount = await conn.ExecuteScalarAsync<int>(adminCountSql, new { AdminRole = RoleIds.Admin }, transaction);
                    if (adminCount <= 1)
                        throw new InvalidOperationException("Admin terakhir tidak dapat dihapus.");
                }

                const string deleteSql = "DELETE FROM user WHERE id = @Id;";
                var affected = await conn.ExecuteAsync(deleteSql, new { Id = id }, transaction);
                await transaction.CommitAsync();
                return affected > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 255)
                throw new ArgumentException("Nama wajib diisi dan maksimal 255 karakter.");
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new ArgumentException("Password minimal 8 karakter.");
        }

        private static void ValidateRole(int roleId)
        {
            if (roleId is < RoleIds.Admin or > RoleIds.DevOps)
                throw new ArgumentException("Role tidak valid.");
        }

        private static int? NormalizeDivision(int? divisionId) => divisionId is > 0 ? divisionId : null;

        private static async Task EnsureNameAvailable(System.Data.Common.DbConnection conn, string name, int? excludedId = null)
        {
            const string sql = @"SELECT COUNT(*) FROM user WHERE nama = @Name AND (@ExcludedId IS NULL OR id <> @ExcludedId);";
            if (await conn.ExecuteScalarAsync<int>(sql, new { Name = name, ExcludedId = excludedId }) > 0)
                throw new ArgumentException("Nama user sudah digunakan.");
        }

        private static async Task ValidateReferences(System.Data.Common.DbConnection conn, int roleId, int? divisionId)
        {
            const string roleSql = "SELECT COUNT(*) FROM role WHERE id = @Id;";
            if (await conn.ExecuteScalarAsync<int>(roleSql, new { Id = roleId }) == 0)
                throw new ArgumentException("Role tidak ditemukan.");

            if (divisionId is > 0)
            {
                const string divisionSql = "SELECT COUNT(*) FROM divisi WHERE id = @Id;";
                if (await conn.ExecuteScalarAsync<int>(divisionSql, new { Id = divisionId }) == 0)
                    throw new ArgumentException("Divisi tidak ditemukan.");
            }
        }
    }
}
