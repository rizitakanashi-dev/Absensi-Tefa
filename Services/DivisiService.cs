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

    public async Task<int> Create(DivisiDTO divisi){
      using var conn = db.connect();
      string sql = @"
        INSERT INTO divisi(nama) VALUES(@nama);";
      return await conn.ExecuteAsync(sql, divisi);
    }

    public async Task<int> Update(DivisiDTO divisi){
      using var conn = db.connect();
      string sql = @"UPDATE divisi SET nama = @nama WHERE id = @id;";
      return await conn.ExecuteAsync(sql, divisi);
    }

    public async Task<int> Delete(int id){
      using var conn = db.connect();
      string sql = @"DELETE FROM divisi WHERE id = @id;";
      return await conn.ExecuteAsync(sql, new { id });
    }
  }
}
