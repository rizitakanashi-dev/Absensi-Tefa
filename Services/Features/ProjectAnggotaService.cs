using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    public class ProjectAnggotaService
    {
        private readonly Database db;

        public ProjectAnggotaService(Database _db)
        {
            db = _db;
        }

        // GET Semua Project Anggota
        public async Task<IEnumerable<ProjectAnggota>> GetAll()
        {
            using var conn = db.connect();
            string sql = @"
                SELECT
                    pa.id AS Id,
                    pa.id_user AS IdUser,
                    pa.id_project AS IdProject,
                    u.nama AS Username,
                    p.nama AS Project
                FROM project_anggota pa
                JOIN user u ON u.id = pa.id_user
                JOIN project p ON p.id = pa.id_project";

            return await conn.QueryAsync<ProjectAnggota>(sql);
        }

        // POST Tambah Anggota Ke Project
        public async Task<bool> Create(ProjectAnggotaDto req)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"
                    INSERT INTO project_anggota (id_user, id_project)
                    VALUES (@User, @Project)";

                int rows = await conn.ExecuteAsync(sql, new { User = req.User, Project = req.Project }, transaction);
                await transaction.CommitAsync();
                return rows > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // DELETE Hapus Anggota Dari Project
        public async Task<bool> Delete(int id)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = "DELETE FROM project_anggota WHERE id = @Id;";
                int rows = await conn.ExecuteAsync(sql, new { Id = id }, transaction);
                await transaction.CommitAsync();
                return rows > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
