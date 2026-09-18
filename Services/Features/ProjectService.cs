using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    public class ProjectService
    {
        private readonly Database db;
        public ProjectService(Database _db) => db = _db;

        public async Task<List<ProjectDTO>> Get()
        {
            using var conn = db.connect();
            string sql = @"SELECT * FROM project;";
            var result = await conn.QueryAsync<ProjectDTO>(sql);
            return result.ToList();
        }

        public async Task<ProjectDTO?> GetById(int id)
        {
            using var conn = db.connect();
            string sql = @"SELECT * FROM project WHERE id = @id;";
            return await conn.QueryFirstOrDefaultAsync<ProjectDTO>(sql, new { id });
        }

        public async Task<int> Create(ProjectDTO project)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"INSERT INTO project(nama) VALUES(@nama);";
                var result = await conn.ExecuteAsync(sql, project, transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> Update(ProjectDTO project)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"UPDATE project SET nama = @nama WHERE id = @id;";
                var result = await conn.ExecuteAsync(sql, project, transaction);
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
                string sql = @"DELETE FROM project WHERE id = @id;";
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
