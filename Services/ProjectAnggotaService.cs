using absensi.models;
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
        public async Task<IEnumerable<projectanggota>> GetAll()
        {
            string sql = @"
                SELECT 
                    pa.id AS id,
                    pa.id_user AS iduser,
                    pa.id_project AS idproject,
                    u.nama AS username,
                    p.nama AS project
                FROM project_anggota pa
                JOIN user u ON u.id = pa.id_user
                JOIN project p ON p.id = pa.id_project";

            return await _db.QueryAsync<projectanggota>(sql);
        }

        // POST Tambah Anggota Ke Project
        public async Task<bool> Create(projectanggotadto req)
        {
            string sql = @"
                INSERT INTO project_anggota (id_user, id_project) 
                VALUES (@user, @project)";

            int rows = await _db.ExecuteAsync(sql, new { user = req.user, project = req.project });
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
