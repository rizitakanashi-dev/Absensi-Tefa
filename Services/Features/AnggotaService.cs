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
                    id_role = 4,
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
                           WHERE u.id_role = 4;";

            var result = await conn.QueryAsync<UserDTO>(sql);
            return result.ToList();
        }
    }
}
