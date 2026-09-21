using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    public class AnggotaService
    {
        private readonly Database db;
        private readonly IPasswordService passwordService;

        public AnggotaService(Database _db, IPasswordService _passwordService)
        {
            db = _db;
            passwordService = _passwordService;
        }

        public async Task<bool> Register(UserOTD data)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string hashedPassword = passwordService.HashPassword(data.password);
                int? divisiId = data.id_divisi > 0 ? data.id_divisi : null;

                string sql = @"INSERT INTO user(nama, password, id_role, id_divisi)
                               VALUES(@nama, @password, @id_role, @id_divisi);";

                int affectedRows = await conn.ExecuteAsync(sql, new
                {
                    nama = data.nama,
                    password = hashedPassword,
                    id_role = RoleIds.Anggota,
                    id_divisi = divisiId
                }, transaction);

                await transaction.CommitAsync();
                return affectedRows > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<UserDTO>> GetAll()
        {
            using var conn = db.connect();
            string sql = @"SELECT u.id, u.nama, r.nama AS role, d.nama AS divisi
                           FROM user u
                           JOIN role r ON r.id = u.id_role
                           LEFT JOIN divisi d ON d.id = u.id_divisi
                           WHERE u.id_role = @RoleAnggota;";

            var result = await conn.QueryAsync<UserDTO>(sql, new { RoleAnggota = RoleIds.Anggota });
            return result.ToList();
        }

        public async Task<UserDTO?> GetById(int id)
        {
            using var conn = db.connect();
            string sql = @"SELECT u.id, u.nama, r.nama AS role, d.nama AS divisi
                           FROM user u
                           JOIN role r ON r.id = u.id_role
                           LEFT JOIN divisi d ON d.id = u.id_divisi
                           WHERE u.id = @id AND u.id_role = @RoleAnggota;";

            return await conn.QueryFirstOrDefaultAsync<UserDTO>(sql, new { id, RoleAnggota = RoleIds.Anggota });
        }

        public async Task<bool> Update(int id, UserOTD data)
        {
            using var conn = db.connect();
            int? divisiId = data.id_divisi > 0 ? data.id_divisi : null;

            string sql = @"UPDATE user 
                           SET nama = @nama, 
                               id_divisi = @id_divisi 
                           WHERE id = @id AND id_role = @RoleAnggota;";

            var result = await conn.ExecuteAsync(sql, new
            {
                id,
                nama = data.nama,
                id_divisi = divisiId,
                RoleAnggota = RoleIds.Anggota
            });

            return result > 0;
        }

        public async Task<bool> Delete(int id)
        {
            using var conn = db.connect();
            string sql = "DELETE FROM user WHERE id = @id AND id_role = @RoleAnggota;";
            var result = await conn.ExecuteAsync(sql, new { id, RoleAnggota = RoleIds.Anggota });

            return result > 0;
        }
    }
}
