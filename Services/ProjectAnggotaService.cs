using Absensi.Models;
using System.Data;
using Dapper;

namespace Absensi.Services
{
    public class ProjectAnggotaService
    {
        private readonly IDbConnection _db;

        public ProjectAnggotaService(IDbConnection db)
        {
            _db = db;
        }

        // GET Semua Project Anggota
        public async Task<IEnumerable<ProjectAnggota>> GetAll()
        {
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

            return await _db.QueryAsync<ProjectAnggota>(sql);
        }

        // POST Tambah Anggota Ke Project
        public async Task<bool> Create(ProjectAnggotaDto req)
        {
            string sql = @"
                INSERT INTO project_anggota (id_user, id_project) 
                VALUES (@User, @Project)";

            int rows = await _db.ExecuteAsync(sql, new { User = req.User, Project = req.Project });
            return rows > 0;
        }

        // DELETE Hapus Anggota Dari Project
        public async Task<bool> Delete(int id)
        {
            string sql = "DELETE FROM project_anggota WHERE id = @Id;";
            int rows = await _db.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }
    }
}
