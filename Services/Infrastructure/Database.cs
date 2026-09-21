using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace Absensi.Services
{
    public class Database
    {
        private readonly IConfiguration _config;

        public Database(IConfiguration config)
        {
            _config = config;
        }

        public MySqlConnection connect()
        {
            var connectionString = _config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' tidak ditemukan. " +
                    "Pastikan appsettings.json sudah dikonfigurasi.");

            return new MySqlConnection(connectionString);
        }
    }
}
