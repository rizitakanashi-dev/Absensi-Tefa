using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    public class PMService
    {
        private readonly Database db;

        public PMService(Database _db) => db = _db;

        public async Task<bool> Register(UserOTD data)
        {
            using (var conn = db.connect())
            {
                await conn.OpenAsync();

                using (var transaction = await conn.BeginTransactionAsync())
                {
                    try
                    {
                        string sql = @"INSERT INTO user(nama, password, id_role, id_divisi)
                           VALUES(@nama, @password, @id_divisi, @id_role);";
                        int affectedRows = await conn.ExecuteAsync(sql, new
                        {
                            nama = data.nama,
                            password = data.password,
                            id_role = 2,
                            id_divisi = data.id_divisi
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
            }
        }

        public async Task<List<UserDTO>> GetAll()
        {
            using (var conn = db.connect())
            {
                string sql = @"SELECT u.id, u.nama, r.nama AS role, d.nama AS divisi
                       FROM user u
                       JOIN role r ON r.id = u.id_role
                       LEFT JOIN divisi d ON d.id = u.id_divisi
                       WHERE u.id_role = 2;";
                var result = await conn.QueryAsync<UserDTO>(sql);
                return result.ToList();
            }
        }
    }
}
