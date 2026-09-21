using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    public class DivisiService
    {
        private readonly Database db;
        public DivisiService(Database _db) => db = _db;

        public async Task<List<DivisiDTO>> Get()
        {
            using var conn = db.connect();
            string sql = @"
        SELECT * FROM divisi;";
            var result = await conn.QueryAsync<DivisiDTO>(sql);
            return result.ToList();
        }

        public async Task<DivisiDTO?> GetById(int id)
        {
            using var conn = db.connect();
            string sql = @"SELECT * FROM divisi WHERE id = @id;";
            return await conn.QueryFirstOrDefaultAsync<DivisiDTO>(sql, new { id });
        }

        public async Task<int> Create(DivisiDTO divisi)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"INSERT INTO divisi(nama) VALUES(@nama);";
                var result = await conn.ExecuteAsync(sql, divisi, transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> Update(DivisiDTO divisi)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"UPDATE divisi SET nama = @nama WHERE id = @id;";
                var result = await conn.ExecuteAsync(sql, divisi, transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> Delete(int id)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"DELETE FROM divisi WHERE id = @id;";
                var result = await conn.ExecuteAsync(sql, new { id }, transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
