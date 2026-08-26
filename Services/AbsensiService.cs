using Absensi.Models;
using System.Data;
using Dapper;

namespace Absensi.Services
{
    public class AbsensiService
    {
        private readonly IDbConnection _db;

        public AbsensiService(IDbConnection db)
        {
            _db = db;
        }

        public async Task<IEnumerable<AbsenRekapDTO>> GetRekapByTanggal(string tanggal)
        {
            string sql = @"
                SELECT 
                    a.id AS IdAbsensi,
                    DATE_FORMAT(a.tanggal, '%Y-%m-%d') AS Tanggal,
                    u.nama AS Nama,
                    COALESCE(d.nama, '-') AS Divisi,
                    COALESCE(p.nama, '-') AS Project,
                    COALESCE(t.target, '-') AS Target,
                    COALESCE(s.nama, 'Null') AS Status,
                    TIME_FORMAT(a.jam_masuk, '%H:%i') AS JamMasuk,
                    TIME_FORMAT(a.jam_pulang, '%H:%i') AS JamPulang
                FROM absensi a
                LEFT JOIN target t ON t.id = a.id_target
                LEFT JOIN user u ON u.id = t.id_user
                LEFT JOIN divisi d ON d.id = u.id_divisi
                LEFT JOIN project p ON p.id = t.id_project
                LEFT JOIN status s ON s.id = t.id_status
                WHERE a.tanggal = @Tanggal AND u.id_role != 1";

            return await _db.QueryAsync<AbsenRekapDTO>(sql, new { Tanggal = tanggal });
        }

        public async Task<bool> AbsenMasuk(int idUser, AbsenMasukDTO req)
        {
            // Buka koneksi manual agar LAST_INSERT_ID() membaca sesi koneksi yang sama
            if (_db.State != ConnectionState.Open) _db.Open();

            string sqlInsertTarget = @"
                INSERT INTO target(id_user, id_project, target, id_status)
                VALUES(@IdUser, @IdProject, @Target, @IdStatus);";

            await _db.ExecuteAsync(sqlInsertTarget, new
            {
                IdUser = idUser,
                req.IdProject,
                req.Target,
                req.IdStatus
            });

            // Ambil ID dari sesi koneksi yang aktif
            int idTarget = await _db.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID();");

            string sqlInsertAbsen = @"
                INSERT INTO absensi(tanggal, id_target, jam_masuk)
                VALUES(CURRENT_DATE(), @IdTarget, CURRENT_TIME());";

            int rows = await _db.ExecuteAsync(sqlInsertAbsen, new { IdTarget = idTarget });
            return rows > 0;
        }

        public async Task<bool> AbsenPulang(AbsenPulangDTO req)
        {
            string sqlStatus = "UPDATE target SET id_status = @IdStatus WHERE id = @IdTarget;";
            await _db.ExecuteAsync(sqlStatus, new { req.IdStatus, req.IdTarget });

            string sqlAbsensi = "UPDATE absensi SET jam_pulang = CURRENT_TIME() WHERE id = @IdAbsensi;";
            int rows = await _db.ExecuteAsync(sqlAbsensi, new { req.IdAbsensi });

            return rows > 0;
        }
    }
}
