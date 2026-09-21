namespace Absensi.Models
{
    public class Target
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public int IdProject { get; set; }
        public string TargetDescription { get; set; } = string.Empty;
        public int IdStatus { get; set; }

        // Navigation properties for joined data
        public string? UserName { get; set; }
        public string? ProjectName { get; set; }
        public string? StatusName { get; set; }
    }

    public class TargetCreateDTO
    {
        public int IdUser { get; set; }
        public int IdProject { get; set; }
        public string Target { get; set; } = string.Empty;
        public int IdStatus { get; set; }
    }

    public class TargetUpdateDTO
    {
        public int Id { get; set; }
        public int? IdProject { get; set; }
        public string? Target { get; set; }
        public int? IdStatus { get; set; }
    }

    public class TargetDTO
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int IdProject { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public int IdStatus { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }
}
