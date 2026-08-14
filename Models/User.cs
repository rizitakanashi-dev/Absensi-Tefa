namespace Absensi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Nama { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // hashed, jangan plain text

        // Foreign keys
        public int IdRole { get; set; }
        public int IdDivisi { get; set; }

        // Navigation properties (relasi ke tabel lain)

        public string Role { get; set; } = string.Empty;
        public string Divisi { get; set; } = string.Empty;
    }
    public class UserOTD
    {
        public string nama { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public int id_role { get; set; }
        public int id_divisi { get; set; }
    }

    public class UserDTO
    {
        public int id { get; set; }
        public string nama { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Divisi { get; set; } = string.Empty;
    }

}
