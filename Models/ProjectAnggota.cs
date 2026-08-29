namespace Absensi.Models
{
    public class ProjectAnggota
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public int IdProject { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
    }

    public class ProjectAnggotaOtd
    {
        public int IdUser { get; set; }
        public int IdProject { get; set; }
    }

    public class ProjectAnggotaDto
    {
        public int User { get; set; }
        public int Project { get; set; } 
    }
}
