namespace Absensi.Models
{
    public class AdminUserCreateDTO
    {
        public string Nama { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int IdRole { get; set; }
        public int? IdDivisi { get; set; }
    }

    public class AdminUserUpdateDTO
    {
        public string Nama { get; set; } = string.Empty;
        public string? Password { get; set; }
        public int IdRole { get; set; }
        public int? IdDivisi { get; set; }
    }
}
