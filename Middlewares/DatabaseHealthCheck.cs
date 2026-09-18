using Microsoft.Extensions.Diagnostics.HealthChecks;
using Absensi.Services;

namespace Absensi.Middlewares
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly Database _db;

        public DatabaseHealthCheck(Database db)
        {
            _db = db;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var conn = _db.connect();
                await conn.OpenAsync(cancellationToken);

                // Simple query to check DB is responsive
                var command = conn.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);

                return HealthCheckResult.Healthy("Database connection is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Database connection failed",
                    exception: ex);
            }
        }
    }
}
