using Dapper;
using Absensi.Models;
namespace Absensi.Services
{
    public class AuthServices
    {
        private readonly Database db;
        public AuthServices(Database _db) => db = _db;

        public async Task<bool> AdminRegister(AdminOTD data)
        {
            using var conn = db.connect();
            string sql = @"
           INSERT INTO user(nama, password, id_role, id_divisi) VALUES(@nama, @password, @id_role, @id_divisi);
           ";
            var result = await conn.ExecuteAsync(sql, new
            {
                nama = data.nama,
                password = data.password,
                id_role = 1,
                id_divisi = 2
            });
            return result > 0;
        }

        public async Task<bool> IsRegistered(){
          using var conn = db.connect();
          var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM user;");
          return count > 0;
        }
    }
}
