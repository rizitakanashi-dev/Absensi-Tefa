using Dapper;
using MySql.Data.MySqlClient;

namespace Absensi.Services
{
    /// <summary>
    /// Memastikan schema/seed yang ditambahkan setelah volume DB sudah ada
    /// tetap terpasang (docker init hanya jalan sekali).
    /// Dilengkapi retry agar MySQL yang sesaat belum siap saat startup
    /// tidak mematikan proses (sebelumnya crash-loop).
    /// </summary>
    public static class DatabaseBootstrap
    {
        /// <summary>
        /// Jeda antar percobaan. Total menunggu ~= 2m sebelum menyerah.
        /// </summary>
        private static readonly TimeSpan[] RetryDelays =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(45)
        ];

        public static async Task EnsureAsync(IConfiguration config, ILogger logger, CancellationToken ct = default)
        {
            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' tidak ditemukan.");

            Exception? lastFailure = null;

            // attempt 0..RetryDelays.Length => jumlah percobaan = length + 1
            for (var attempt = 0; attempt <= RetryDelays.Length; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // Koneksi dibuat ulang tiap percobaan: koneksi yang gagal
                    // terbuka (MySQL belum siap / sempat drop) bisa dalam state tak valid.
                    await using var conn = new MySqlConnection(connectionString);
                    await conn.OpenAsync(ct);

                    await EnsureSchemaAsync(conn, logger, ct);

                    logger.LogInformation("Database bootstrap selesai (roles + divisi + status + hosting_request).");
                    return;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    lastFailure = ex;

                    if (attempt >= RetryDelays.Length)
                        break;

                    logger.LogWarning(
                        "Database belum siap (percobaan {Attempt}/{MaxAttempts}): {Message}. Coba lagi dalam {Delay}s.",
                        attempt + 1, RetryDelays.Length + 1, ex.Message, RetryDelays[attempt].TotalSeconds);

                    await Task.Delay(RetryDelays[attempt], ct);
                }
            }

            throw new InvalidOperationException(
                "Gagal melakukan database bootstrap setelah beberapa percobaan. Periksa ketersediaan MySQL.",
                lastFailure);
        }

        private static async Task EnsureSchemaAsync(MySqlConnection conn, ILogger logger, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Seed roles. INSERT IGNORE hanya mengisi yang belum ada; baris yang
            // id-nya sudah ada tapi namanya salah (mis. volume lama sebelum DevOps,
            // atau partial seed) tidak akan diperbaiki — karenanya UPDATE by id di bawah.
            await conn.ExecuteAsync(@"
                INSERT IGNORE INTO `role` (`id`, `nama`) VALUES
                    (1, 'Admin'), (2, 'PM'), (3, 'Guru'), (4, 'Anggota'), (5, 'DevOps');");

            // RoleIds dikode keras by-id (Admin=1..DevOps=5) dan dipakai banyak
            // query (mis. user.id_role = 5 untuk DevOps). Role id tsb HARUS bernama
            // benar agar sistem berfungsi, jadi nama seed dikembalikan ke kanoniknya.
            await conn.ExecuteAsync(@"
                UPDATE `role` SET `nama` = 'Admin'   WHERE `id` = 1 AND `nama` <> 'Admin';
                UPDATE `role` SET `nama` = 'PM'      WHERE `id` = 2 AND `nama` <> 'PM';
                UPDATE `role` SET `nama` = 'Guru'    WHERE `id` = 3 AND `nama` <> 'Guru';
                UPDATE `role` SET `nama` = 'Anggota' WHERE `id` = 4 AND `nama` <> 'Anggota';
                UPDATE `role` SET `nama` = 'DevOps'  WHERE `id` = 5 AND `nama` <> 'DevOps';");

            await conn.ExecuteAsync(@"
                INSERT IGNORE INTO `divisi` (`id`, `nama`) VALUES
                    (1, 'Backend'), (2, 'Frontend'), (3, 'Game');
                UPDATE `divisi` SET `nama` = 'Backend'  WHERE `id` = 1 AND `nama` <> 'Backend';
                UPDATE `divisi` SET `nama` = 'Frontend' WHERE `id` = 2 AND `nama` <> 'Frontend';
                UPDATE `divisi` SET `nama` = 'Game'     WHERE `id` = 3 AND `nama` <> 'Game';");

            await conn.ExecuteAsync(@"
                INSERT IGNORE INTO `status` (`id`, `nama`) VALUES
                    (1, 'Null'), (2, 'On Progress'), (3, 'Done'), (4, 'Izin'), (5, 'Sakit');
                UPDATE `status` SET `nama` = 'Null'       WHERE `id` = 1 AND `nama` <> 'Null';
                UPDATE `status` SET `nama` = 'On Progress' WHERE `id` = 2 AND `nama` <> 'On Progress';
                UPDATE `status` SET `nama` = 'Done'       WHERE `id` = 3 AND `nama` <> 'Done';
                UPDATE `status` SET `nama` = 'Izin'       WHERE `id` = 4 AND `nama` <> 'Izin';
                UPDATE `status` SET `nama` = 'Sakit'      WHERE `id` = 5 AND `nama` <> 'Sakit';");

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
        }
    }
}