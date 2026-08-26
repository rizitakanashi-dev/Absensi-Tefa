namespace Absensi.Models
{
    public class AbsenRekapDTO
    {
        public int IdAbsensi { get; set; }
        public string Tanggal { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;
        public string Divisi { get; set; } = string.Empty;
        public string? Project { get; set; }
        public string? Target { get; set; }
        public string Status { get; set; } = "Null";
        public string? JamMasuk { get; set; }
        public string? JamPulang { get; set; }
    }

    public class AbsenMasukDTO
    {
        public int IdProject { get; set; }
        public string Target { get; set; } = string.Empty;
        public int IdStatus { get; set; }
    }

    public class AbsenPulangDTO
    {
        public int IdAbsensi { get; set; }
        public int IdTarget { get; set; }
        public int IdStatus { get; set; }
    }
}
