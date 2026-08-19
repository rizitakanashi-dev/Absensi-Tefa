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

        public async Task<DivisiDTO?> GetById(int id)
        {
            using var conn = db.connect();
            string sql = @"SELECT * FROM project WHERE id = @id;";
            return await conn.QueryFirstOrDefaultAsync<DivisiDTO>(sql, new { id });
        }

        public async Task<int> Create(ProjectDTO project)
        {
            using var conn = db.connect();
            string sql = @"INSERT INTO project(nama) VALUES(@nama);";
            return await conn.ExecuteAsync(sql, project);
        }

        public async Task<int> Update(ProjectDTO project)
        {
            using var conn = db.connect();
            string sql = @"UPDATE project SET nama = @nama WHERE id = @id;";
            return await conn.ExecuteAsync(sql, project);
        }

        public async Task<int> Delete(int id)
        {
            using var conn = db.connect();
            string sql = @"DELETE FROM project WHERE id = @id;";
            return await conn.ExecuteAsync(sql, new { id });
        }
    }
}
