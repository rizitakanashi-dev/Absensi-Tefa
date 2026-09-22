using System.Text.Json.Serialization;

namespace Absensi.Models
{
    public class AdminUserCreateDTO
    {
        public string Nama { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int IdRole { get; set; }
        public int? IdDivisi { get; set; }

        // Frontend kadang mengirim snake_case (id_role / id_divisi)
        [JsonPropertyName("id_role")]
        public int IdRoleSnake
        {
            get => IdRole;
            set { if (value > 0) IdRole = value; }
        }

        [JsonPropertyName("id_divisi")]
        public int? IdDivisiSnake
        {
            get => IdDivisi;
            set => IdDivisi = value;
        }
    }

    public class AdminUserUpdateDTO
    {
        public string Nama { get; set; } = string.Empty;
        public string? Password { get; set; }
        public int IdRole { get; set; }
        public int? IdDivisi { get; set; }

        [JsonPropertyName("id_role")]
        public int IdRoleSnake
        {
            get => IdRole;
            set { if (value > 0) IdRole = value; }
        }

        [JsonPropertyName("id_divisi")]
        public int? IdDivisiSnake
        {
            get => IdDivisi;
            set => IdDivisi = value;
        }
    }
}
