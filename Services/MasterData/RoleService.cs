using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    public class RoleServices
    {
        private readonly Database db;
        public RoleServices(Database _db) => db = _db;


        public async Task<List<RoleDTO>> Get(){
          using var conn = db.connect();
          string sql = @"
            SELECT * FROM role;";
          var result = await conn.QueryAsync<RoleDTO>(sql);
          return result.ToList();
        }
    }
}
