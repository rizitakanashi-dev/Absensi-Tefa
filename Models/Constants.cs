namespace Absensi.Models
{
    /// <summary>
    /// Constants untuk Role IDs sesuai dengan database seed
    /// </summary>
    public static class RoleIds
    {
        public const int Admin = 1;
        public const int PM = 2;
        public const int Guru = 3;
        public const int Anggota = 4;
    }

    /// <summary>
    /// Constants untuk Status IDs sesuai dengan database seed
    /// </summary>
    public static class StatusIds
    {
        public const int Null = 1;
        public const int OnProgress = 2;
        public const int Done = 3;
        public const int Izin = 4;
        public const int Sakit = 5;
    }
}
