using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
  public class DivisiService
  {
    private readonly Database db;
    public DivisiService(Database _db) => db = _db;

    public async Task<List<DivisiDTO>> Get(){
      using var conn = db.connect();
      string sql = @"
        SELECT * FROM divisi;";
        var result = await conn.QueryAsync<DivisiDTO>(sql);
        return result.ToList();
    }
  }
}
