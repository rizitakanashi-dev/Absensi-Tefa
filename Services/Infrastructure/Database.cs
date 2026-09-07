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
                ?? _config["ConnectionStrings:DefaultConnection"]
                ?? "Server=db;Port=3306;Database=absensi;Uid=absensi_user;Pwd=absensipassword;SslMode=Disabled;AllowPublicKeyRetrieval=True;";

            return new MySqlConnection(connectionString);
        }
    }
}
