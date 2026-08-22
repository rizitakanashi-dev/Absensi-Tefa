using Dapper;
using Absensi.Models;

namespace Absensi.Services
{
    public class AuthServices
    {
        private readonly Database db;
        public AuthServices(Database _db) => db = _db;

        // 1. REGISTER ADMIN
        public async Task<bool> AdminRegister(AdminOTD data)
        {
            using var conn = db.connect();
            string sql = @"INSERT INTO user(nama, password, id_role, id_divisi) 
                           VALUES(@nama, @password, @id_role, @id_divisi);";

            var result = await conn.ExecuteAsync(sql, new
            {
                nama = data.nama,
                password = data.password,
                id_role = 1, // 1 = Admin
                id_divisi = data.id_divisi // Diambil dari DTO (bisa null/diisi)
            });

            return result > 0;
        }

        // 2. CEK STATUS REGISTER
        public async Task<bool> IsRegistered()
        {
            using var conn = db.connect();
            var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM user;");
            return count > 0;
        }

        // 3. LOGIN USER (AMBIL USER UNTUK DICOKKAN PASSWORD)
        public async Task<UserSessionModel?> Login(Login data)
        {
            using var conn = db.connect();
            string sql = @"SELECT u.id, u.nama, u.password, r.nama AS role
                           FROM user u
                           JOIN role r ON r.id = u.id_role
                           WHERE u.nama = @nama;";

            return await conn.QueryFirstOrDefaultAsync<UserSessionModel>(sql, new { nama = data.nama });
        }

        // 4. UPDATE REFRESH TOKEN DI DATABASE
        public async Task UpdateRefreshToken(string refreshToken, DateTime expiredAt, int userId)
        {
            using var conn = db.connect();
            string sql = @"UPDATE user 
                           SET refresh_token = @refreshToken, 
                               refresh_token_expired = @expiredAt 
                           WHERE id = @userId;";

            await conn.ExecuteAsync(sql, new { refreshToken, expiredAt, userId });
        }

        // 5. VALIDASI REFRESH TOKEN
        public async Task<UserSessionModel?> RefreshTokenService(RefreshRequest req)
        {
            using var conn = db.connect();
            string sql = @"SELECT u.id, u.nama, r.nama AS role, u.refresh_token_expired AS refreshTokenExpired
                           FROM user u
                           JOIN role r ON r.id = u.id_role
                           WHERE u.refresh_token = @refreshToken;";

            return await conn.QueryFirstOrDefaultAsync<UserSessionModel>(sql, new { refreshToken = req.RefreshToken });
        }

        // 6. GET PROFIL USER UNTUK ENDPOINT /ME
        public async Task<UserDTO?> GetMe(int userId)
        {
            using var conn = db.connect();
            string sql = @"SELECT u.id, u.nama, r.nama AS Role, d.nama AS Divisi
                           FROM user u
                           JOIN role r ON r.id = u.id_role
                           LEFT JOIN divisi d ON d.id = u.id_divisi
                           WHERE u.id = @userId;";

            return await conn.QueryFirstOrDefaultAsync<UserDTO>(sql, new { userId });
        }
    }
}
