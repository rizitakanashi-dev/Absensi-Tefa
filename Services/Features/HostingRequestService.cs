using Absensi.Models;
using Dapper;

namespace Absensi.Services
{
    public class HostingRequestService
    {
        private readonly Database db;

        public HostingRequestService(Database _db)
        {
            db = _db;
        }

        // GET All hosting requests dengan filter
        public async Task<IEnumerable<HostingRequestListDTO>> GetAll(string? status = null, int? userId = null)
        {
            using var conn = db.connect();

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(status))
            {
                whereClauses.Add("hr.status = @Status");
                parameters.Add("Status", status);
            }

            if (userId.HasValue)
            {
                whereClauses.Add("hr.id_user = @UserId");
                parameters.Add("UserId", userId.Value);
            }

            string whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            string sql = $@"
                SELECT 
                    hr.id AS Id,
                    u.nama AS UserName,
                    p.nama AS ProjectName,
                    hr.contact_name AS ContactName,
                    hr.status AS Status,
                    pm.nama AS PmReviewerName,
                    devops.nama AS DevOpsHandlerName,
                    hr.created_at AS CreatedAt
                FROM hosting_request hr
                LEFT JOIN user u ON u.id = hr.id_user
                LEFT JOIN project p ON p.id = hr.id_project
                LEFT JOIN user pm ON pm.id = hr.id_pm_reviewer
                LEFT JOIN user devops ON devops.id = hr.id_devops_handler
                {whereClause}
                ORDER BY hr.created_at DESC";

            return await conn.QueryAsync<HostingRequestListDTO>(sql, parameters);
        }

        // GET hosting request by ID
        public async Task<HostingRequestDTO?> GetById(int id)
        {
            using var conn = db.connect();
            string sql = @"
                SELECT 
                    hr.id AS Id,
                    hr.id_user AS IdUser,
                    u.nama AS UserName,
                    hr.id_project AS IdProject,
                    p.nama AS ProjectName,
                    hr.contact_name AS ContactName,
                    hr.contact_email AS ContactEmail,
                    hr.contact_phone AS ContactPhone,
                    hr.project_description AS ProjectDescription,
                    hr.tech_stack AS TechStack,
                    hr.repository_url AS RepositoryUrl,
                    hr.documentation_url AS DocumentationUrl,
                    hr.status AS Status,
                    hr.id_pm_reviewer AS IdPmReviewer,
                    pm.nama AS PmReviewerName,
                    hr.pm_notes AS PmNotes,
                    hr.pm_reviewed_at AS PmReviewedAt,
                    hr.id_devops_handler AS IdDevOpsHandler,
                    devops.nama AS DevOpsHandlerName,
                    hr.devops_notes AS DevOpsNotes,
                    hr.hosting_url AS HostingUrl,
                    hr.created_at AS CreatedAt,
                    hr.updated_at AS UpdatedAt
                FROM hosting_request hr
                LEFT JOIN user u ON u.id = hr.id_user
                LEFT JOIN project p ON p.id = hr.id_project
                LEFT JOIN user pm ON pm.id = hr.id_pm_reviewer
                LEFT JOIN user devops ON devops.id = hr.id_devops_handler
                WHERE hr.id = @Id";

            return await conn.QueryFirstOrDefaultAsync<HostingRequestDTO>(sql, new { Id = id });
        }

        // CREATE hosting request
        public async Task<int> Create(int userId, HostingRequestCreateDTO data)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"
                    INSERT INTO hosting_request (
                        id_user, id_project, contact_name, contact_email, contact_phone,
                        project_description, tech_stack, repository_url, documentation_url, status
                    )
                    VALUES (
                        @IdUser, @IdProject, @ContactName, @ContactEmail, @ContactPhone,
                        @ProjectDescription, @TechStack, @RepositoryUrl, @DocumentationUrl, @Status
                    );
                    SELECT LAST_INSERT_ID();";

                var newId = await conn.ExecuteScalarAsync<int>(sql, new
                {
                    IdUser = userId,
                    data.IdProject,
                    data.ContactName,
                    data.ContactEmail,
                    data.ContactPhone,
                    data.ProjectDescription,
                    data.TechStack,
                    data.RepositoryUrl,
                    data.DocumentationUrl,
                    Status = HostingStatus.Pending
                }, transaction);

                await transaction.CommitAsync();
                return newId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // UPDATE hosting request (hanya jika status pending atau rejected)
        public async Task<bool> Update(int id, HostingRequestUpdateDTO data)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"
                    UPDATE hosting_request SET
                        id_project = @IdProject,
                        contact_name = @ContactName,
                        contact_email = @ContactEmail,
                        contact_phone = @ContactPhone,
                        project_description = @ProjectDescription,
                        tech_stack = @TechStack,
                        repository_url = @RepositoryUrl,
                        documentation_url = @DocumentationUrl
                    WHERE id = @Id AND status IN (@StatusPending, @StatusRejected)";

                var result = await conn.ExecuteAsync(sql, new
                {
                    Id = id,
                    data.IdProject,
                    data.ContactName,
                    data.ContactEmail,
                    data.ContactPhone,
                    data.ProjectDescription,
                    data.TechStack,
                    data.RepositoryUrl,
                    data.DocumentationUrl,
                    StatusPending = HostingStatus.Pending,
                    StatusRejected = HostingStatus.Rejected
                }, transaction);

                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // APPROVE hosting request (PM)
        public async Task<bool> Approve(int id, int pmId, string notes)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"
                    UPDATE hosting_request SET
                        status = @Status,
                        id_pm_reviewer = @PmId,
                        pm_notes = @Notes,
                        pm_reviewed_at = @ReviewedAt
                    WHERE id = @Id AND status = @StatusPending";

                var result = await conn.ExecuteAsync(sql, new
                {
                    Id = id,
                    Status = HostingStatus.Approved,
                    PmId = pmId,
                    Notes = notes,
                    ReviewedAt = DateTime.UtcNow,
                    StatusPending = HostingStatus.Pending
                }, transaction);

                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // REJECT hosting request (PM)
        public async Task<bool> Reject(int id, int pmId, string notes)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"
                    UPDATE hosting_request SET
                        status = @Status,
                        id_pm_reviewer = @PmId,
                        pm_notes = @Notes,
                        pm_reviewed_at = @ReviewedAt
                    WHERE id = @Id AND status = @StatusPending";

                var result = await conn.ExecuteAsync(sql, new
                {
                    Id = id,
                    Status = HostingStatus.Rejected,
                    PmId = pmId,
                    Notes = notes,
                    ReviewedAt = DateTime.UtcNow,
                    StatusPending = HostingStatus.Pending
                }, transaction);

                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // START processing (DevOps)
        public async Task<bool> StartProcessing(int id, int devopsId, bool isAdmin = false)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"
                    UPDATE hosting_request SET
                        status = @Status,
                        id_devops_handler = @DevOpsId
                    WHERE id = @Id
                      AND status = @StatusApproved
                      AND (@IsAdmin = 1 OR EXISTS (
                          SELECT 1 FROM user
                          WHERE user.id = @DevOpsId AND user.id_role = @RoleDevOps
                      ));";

                var result = await conn.ExecuteAsync(sql, new
                {
                    Id = id,
                    Status = HostingStatus.InProgress,
                    DevOpsId = devopsId,
                    IsAdmin = isAdmin ? 1 : 0,
                    RoleDevOps = RoleIds.DevOps,
                    StatusApproved = HostingStatus.Approved
                }, transaction);

                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // COMPLETE hosting (DevOps)
        public async Task<bool> Complete(int id, HostingRequestDevOpsUpdateDTO data, int devopsId, bool isAdmin = false)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"
                    UPDATE hosting_request SET
                        status = @Status,
                        devops_notes = @DevOpsNotes,
                        hosting_url = @HostingUrl
                    WHERE id = @Id
                      AND status = @StatusInProgress
                      AND (@IsAdmin = 1 OR id_devops_handler = @DevOpsId);";

                var result = await conn.ExecuteAsync(sql, new
                {
                    Id = id,
                    Status = HostingStatus.Completed,
                    data.DevOpsNotes,
                    data.HostingUrl,
                    DevOpsId = devopsId,
                    IsAdmin = isAdmin ? 1 : 0,
                    StatusInProgress = HostingStatus.InProgress
                }, transaction);

                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // UPDATE DevOps notes (DevOps)
        public async Task<bool> UpdateDevOpsNotes(int id, string notes, int devopsId, bool isAdmin = false)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"
                    UPDATE hosting_request SET
                        devops_notes = @Notes
                    WHERE id = @Id
                      AND status IN (@StatusInProgress, @StatusCompleted)
                      AND (@IsAdmin = 1 OR id_devops_handler = @DevOpsId)";

                var result = await conn.ExecuteAsync(sql, new
                {
                    Id = id,
                    Notes = notes,
                    DevOpsId = devopsId,
                    IsAdmin = isAdmin ? 1 : 0,
                    StatusInProgress = HostingStatus.InProgress,
                    StatusCompleted = HostingStatus.Completed
                }, transaction);

                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // CANCEL request (soft delete)
        public async Task<bool> Cancel(int id)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = @"
                    UPDATE hosting_request SET
                        status = @Status
                    WHERE id = @Id AND status IN (@StatusPending, @StatusRejected)";

                var result = await conn.ExecuteAsync(sql, new
                {
                    Id = id,
                    Status = HostingStatus.Cancelled,
                    StatusPending = HostingStatus.Pending,
                    StatusRejected = HostingStatus.Rejected
                }, transaction);

                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // DELETE (hard delete - Admin only)
        public async Task<bool> Delete(int id)
        {
            using var conn = db.connect();
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                string sql = "DELETE FROM hosting_request WHERE id = @Id";
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
    }
}
