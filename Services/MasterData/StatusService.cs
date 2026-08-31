using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
  public class StatusService
  {
    private readonly Database db;
    public StatusService(Database _db) => db = _db;

    public async Task<List<StatusDTO>> Get(){
      using var conn = db.connect();
      string sql = @"
        SELECT * FROM status;";
        var result = await conn.QueryAsync<StatusDTO>(sql);
        return result.ToList();
    }
  }
}
