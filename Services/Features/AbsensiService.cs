using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    public class AbsensiService
    {
        private readonly Database db;

        public AbsensiService(Database _db)
        {
            db = _db;
        }

        public async Task<IEnumerable<AbsenRekapDTO>> GetRekapByTanggal(string tanggal, int page, int pageSize)
        {
            using var conn = db.connect();
            int offset = (page - 1) * pageSize;

            string sql = @"
                SELECT
                    a.id AS IdAbsensi,
                    t.id AS IdTarget,
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
                WHERE a.tanggal = @Tanggal AND u.id_role != @AdminRole
                ORDER BY a.id DESC
                LIMIT @PageSize OFFSET @Offset";

            return await conn.QueryAsync<AbsenRekapDTO>(sql, new
            {
                Tanggal = tanggal,
                AdminRole = RoleIds.Admin,
                PageSize = pageSize,
                Offset = offset
            });
        }

        public async Task<int> GetRekapCount(string tanggal)
        {
            using var conn = db.connect();
            string sql = @"
                SELECT COUNT(*)
                FROM absensi a
                LEFT JOIN target t ON t.id = a.id_target
                LEFT JOIN user u ON u.id = t.id_user
                WHERE a.tanggal = @Tanggal AND u.id_role != @AdminRole";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                Tanggal = tanggal,
                AdminRole = RoleIds.Admin
            });
        }

        public async Task<bool> AbsenMasuk(int idUser, AbsenMasukDTO req)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sqlInsertTarget = @"
                    INSERT INTO target(id_user, id_project, target, id_status)
                    VALUES(@IdUser, @IdProject, @Target, @IdStatus);";

                await conn.ExecuteAsync(sqlInsertTarget, new
                {
                    IdUser = idUser,
                    req.IdProject,
                    req.Target,
                    req.IdStatus
                }, transaction);

                int idTarget = await conn.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID();", transaction: transaction);

                string sqlInsertAbsen = @"
                    INSERT INTO absensi(tanggal, id_target, jam_masuk)
                    VALUES(CURRENT_DATE(), @IdTarget, CURRENT_TIME());";

                int rows = await conn.ExecuteAsync(sqlInsertAbsen, new { IdTarget = idTarget }, transaction);
                await transaction.CommitAsync();
                return rows > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> AbsenPulang(int userId, AbsenPulangDTO req)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                // Update status + jam pulang hanya jika absensi milik user yang login
                // dan IdTarget cocok dengan baris absensi tersebut.
                string sql = @"
                    UPDATE absensi a
                    INNER JOIN target t ON t.id = a.id_target
                    SET a.jam_pulang = CURRENT_TIME(),
                        t.id_status = @IdStatus
                    WHERE a.id = @IdAbsensi
                      AND a.id_target = @IdTarget
                      AND t.id_user = @UserId
                      AND a.jam_pulang IS NULL;";

                int rows = await conn.ExecuteAsync(sql, new
                {
                    req.IdAbsensi,
                    req.IdTarget,
                    req.IdStatus,
                    UserId = userId
                }, transaction);

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
