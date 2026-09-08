using Dapper;
using Absensi.Models;

namespace Absensi.Services
{
    public class GuruService
    {
        private readonly Database db;
        private readonly IPasswordService passwordService;

        public GuruService(Database _db, IPasswordService _passwordService)
        {
            db = _db;
            passwordService = _passwordService;
        }

        public async Task<IEnumerable<UserDTO>> GetAll()
        {
            using var conn = db.connect();
            string sql = @"SELECT u.id, u.nama, r.nama AS Role, d.nama AS Divisi
                           FROM user u
                           JOIN role r ON r.id = u.id_role
                           LEFT JOIN divisi d ON d.id = u.id_divisi
                           WHERE r.nama = 'Guru';";

            return await conn.QueryAsync<UserDTO>(sql);
        }

        public async Task<UserDTO?> GetById(int id)
        {
            using var conn = db.connect();
            string sql = @"SELECT u.id, u.nama, r.nama AS Role, d.nama AS Divisi
                           FROM user u
                           JOIN role r ON r.id = u.id_role
                           LEFT JOIN divisi d ON d.id = u.id_divisi
                           WHERE u.id = @id AND r.nama = 'Guru';";

            return await conn.QueryFirstOrDefaultAsync<UserDTO>(sql, new { id });
        }

        public async Task<bool> Create(UserOTD data)
        {
            using var conn = db.connect();

            string hashedPassword = passwordService.HashPassword(data.password);
            int? divisiId = data.id_divisi > 0 ? data.id_divisi : null;

            string sql = @"INSERT INTO user(nama, password, id_role, id_divisi) 
                           VALUES(@nama, @password, @id_role, @id_divisi);";

            var result = await conn.ExecuteAsync(sql, new
            {
                nama = data.nama,
                password = hashedPassword,
                id_role = 3,
                id_divisi = divisiId
            });

            return result > 0;
        }

        public async Task<bool> Update(int id, UserOTD data)
        {
            using var conn = db.connect();

            int? divisiId = data.id_divisi > 0 ? data.id_divisi : null;

            string sql = @"UPDATE user 
                           SET nama = @nama, 
                               id_role = @id_role, 
                               id_divisi = @id_divisi 
                           WHERE id = @id;";

            var result = await conn.ExecuteAsync(sql, new
            {
                id,
                nama = data.nama,
                id_role = 3,
                id_divisi = divisiId
            });

            return result > 0;
        }

        public async Task<bool> Delete(int id)
        {
            using var conn = db.connect();
            string sql = "DELETE FROM user WHERE id = @id;";
            var result = await conn.ExecuteAsync(sql, new { id });

            return result > 0;
        }
    }
}
