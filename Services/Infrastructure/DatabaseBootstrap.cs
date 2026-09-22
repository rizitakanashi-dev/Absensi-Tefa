using Dapper;
using MySql.Data.MySqlClient;

namespace Absensi.Services
{
    /// <summary>
    /// Memastikan schema/seed yang ditambahkan setelah volume DB sudah ada
    /// tetap terpasang (docker init hanya jalan sekali).
    /// </summary>
    public static class DatabaseBootstrap
    {
        public static async Task EnsureAsync(IConfiguration config, ILogger logger, CancellationToken ct = default)
        {
            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' tidak ditemukan.");

            await using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync(ct);

            // Seed roles (termasuk DevOps yang sering hilang di DB lama)
            await conn.ExecuteAsync(@"
                INSERT IGNORE INTO `role` (`id`, `nama`) VALUES
                    (1, 'Admin'), (2, 'PM'), (3, 'Guru'), (4, 'Anggota'), (5, 'DevOps');");
            await conn.ExecuteAsync(@"
                INSERT IGNORE INTO `divisi` (`id`, `nama`) VALUES
                    (1, 'Backend'), (2, 'Frontend'), (3, 'Game');");
            await conn.ExecuteAsync(@"
                INSERT IGNORE INTO `status` (`id`, `nama`) VALUES
                    (1, 'Null'), (2, 'On Progress'), (3, 'Done'), (4, 'Izin'), (5, 'Sakit');");

            // Tabel hosting_request (ditambahkan setelah setup awal)
            await conn.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS `hosting_request` (
                  `id` INT(11) NOT NULL AUTO_INCREMENT,
                  `id_user` INT(11) NOT NULL,
                  `id_project` INT(11) NOT NULL,
                  `contact_name` VARCHAR(255) NOT NULL,
                  `contact_email` VARCHAR(255) NOT NULL,
                  `contact_phone` VARCHAR(50) NOT NULL,
                  `project_description` TEXT,
                  `tech_stack` TEXT,
                  `repository_url` VARCHAR(500),
                  `documentation_url` VARCHAR(500),
                  `status` ENUM('pending','approved','rejected','in_progress','completed','cancelled') DEFAULT 'pending',
                  `id_pm_reviewer` INT(11) DEFAULT NULL,
                  `pm_notes` TEXT,
                  `pm_reviewed_at` DATETIME DEFAULT NULL,
                  `id_devops_handler` INT(11) DEFAULT NULL,
                  `devops_notes` TEXT,
                  `hosting_url` VARCHAR(500),
                  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                  PRIMARY KEY (`id`),
                  KEY `fk_hr_user` (`id_user`),
                  KEY `fk_hr_project` (`id_project`),
                  KEY `fk_hr_pm` (`id_pm_reviewer`),
                  KEY `fk_hr_devops` (`id_devops_handler`),
                  CONSTRAINT `fk_hr_user` FOREIGN KEY (`id_user`) REFERENCES `user` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
                  CONSTRAINT `fk_hr_project` FOREIGN KEY (`id_project`) REFERENCES `project` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
                  CONSTRAINT `fk_hr_pm` FOREIGN KEY (`id_pm_reviewer`) REFERENCES `user` (`id`) ON DELETE SET NULL ON UPDATE CASCADE,
                  CONSTRAINT `fk_hr_devops` FOREIGN KEY (`id_devops_handler`) REFERENCES `user` (`id`) ON DELETE SET NULL ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            logger.LogInformation("Database bootstrap selesai (roles + hosting_request).");
        }
    }
}
