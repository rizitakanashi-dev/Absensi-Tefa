using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    public class TargetService
    {
        private readonly Database db;

        public TargetService(Database _db)
        {
            db = _db;
        }

        // GET All Targets dengan filter optional
        public async Task<IEnumerable<TargetDTO>> GetAll(int? userId = null, int? projectId = null, int? statusId = null)
        {
            using var conn = db.connect();
            
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (userId.HasValue)
            {
                whereClauses.Add("t.id_user = @UserId");
                parameters.Add("UserId", userId.Value);
            }

            if (projectId.HasValue)
            {
                whereClauses.Add("t.id_project = @ProjectId");
                parameters.Add("ProjectId", projectId.Value);
            }

            if (statusId.HasValue)
            {
                whereClauses.Add("t.id_status = @StatusId");
                parameters.Add("StatusId", statusId.Value);
            }

            string whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            string sql = $@"
                SELECT 
                    t.id AS Id,
                    t.id_user AS IdUser,
                    u.nama AS UserName,
                    t.id_project AS IdProject,
                    p.nama AS ProjectName,
                    t.target AS Target,
                    t.id_status AS IdStatus,
                    s.nama AS StatusName
                FROM target t
                LEFT JOIN user u ON u.id = t.id_user
                LEFT JOIN project p ON p.id = t.id_project
                LEFT JOIN status s ON s.id = t.id_status
                {whereClause}
                ORDER BY t.id DESC";

            return await conn.QueryAsync<TargetDTO>(sql, parameters);
        }

        // GET Target by ID
        public async Task<TargetDTO?> GetById(int id)
        {
            using var conn = db.connect();
            string sql = @"
                SELECT 
                    t.id AS Id,
                    t.id_user AS IdUser,
                    u.nama AS UserName,
                    t.id_project AS IdProject,
                    p.nama AS ProjectName,
                    t.target AS Target,
                    t.id_status AS IdStatus,
                    s.nama AS StatusName
                FROM target t
                LEFT JOIN user u ON u.id = t.id_user
                LEFT JOIN project p ON p.id = t.id_project
                LEFT JOIN status s ON s.id = t.id_status
                WHERE t.id = @Id";

            return await conn.QueryFirstOrDefaultAsync<TargetDTO>(sql, new { Id = id });
        }

        // CREATE Target
        public async Task<int> Create(TargetCreateDTO data)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"
                    INSERT INTO target (id_user, id_project, target, id_status)
                    VALUES (@IdUser, @IdProject, @Target, @IdStatus);
                    SELECT LAST_INSERT_ID();";

                var newId = await conn.ExecuteScalarAsync<int>(sql, data, transaction);
                await transaction.CommitAsync();
                return newId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // UPDATE Target
        public async Task<bool> Update(TargetUpdateDTO data)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                var setClauses = new List<string>();
                var parameters = new DynamicParameters();
                parameters.Add("Id", data.Id);

                if (data.IdProject.HasValue)
                {
                    setClauses.Add("id_project = @IdProject");
                    parameters.Add("IdProject", data.IdProject.Value);
                }

                if (!string.IsNullOrEmpty(data.Target))
                {
                    setClauses.Add("target = @Target");
                    parameters.Add("Target", data.Target);
                }

                if (data.IdStatus.HasValue)
                {
                    setClauses.Add("id_status = @IdStatus");
                    parameters.Add("IdStatus", data.IdStatus.Value);
                }

                if (!setClauses.Any())
                    return false;

                string sql = $"UPDATE target SET {string.Join(", ", setClauses)} WHERE id = @Id";
                var result = await conn.ExecuteAsync(sql, parameters, transaction);
                
                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // DELETE Target
        public async Task<bool> Delete(int id)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = "DELETE FROM target WHERE id = @Id";
                var result = await conn.ExecuteAsync(sql, new { Id = id }, transaction);
                
                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // GET Targets by User (untuk user melihat target mereka sendiri)
        public async Task<IEnumerable<TargetDTO>> GetByUserId(int userId)
        {
            return await GetAll(userId: userId);
        }
    }
}
