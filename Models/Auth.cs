using Microsoft.AspNetCore.Authorization;

namespace Absensi.Models
{
  public class AdminOTD{
    public string nama {get;set;} = string.Empty;
    public string password {get;set;} = string.Empty;
    public int id_role {get;set;}
    public int? id_divisi {get;set;}
  }

  public class Login
  {
    public string nama {get; set;} = string.Empty;
    public string password {get; set;} = string.Empty;
  }

  public class LoginResponse
  {
      public string Token {get; set;} = string.Empty;
      public string Nama {get; set;} = string.Empty;
      public string Role {get; set;} = string.Empty;
      public string Refresh_Token {get; set;} = string.Empty;
  }

  public class UserSessionModel
  {
      public int id { get; set; }
      public string nama { get; set; } = string.Empty;
      public string password { get; set; } = string.Empty;
      public string role { get; set; } = string.Empty;
      public DateTime? refreshTokenExpired { get; set; }
  }

  public class RefreshRequest
  {
      public string RefreshToken { get; set; } = string.Empty;
  }

  public static class Policies
  {
    public const string Admin = "Admin";
    public const string ProjectManager = "PM";
    public const string Guru = "Guru";
    public const string Anggota = "Anggota";
    public const string DevOps = "DevOps";

    public static void Register(AuthorizationOptions options)
    {
      options.AddPolicy(Admin, p => p.RequireRole("Admin"));
      options.AddPolicy(ProjectManager, p => p.RequireRole("PM"));
      options.AddPolicy(Guru, p => p.RequireRole("Guru"));
      options.AddPolicy(Anggota, p => p.RequireRole("Anggota"));
      options.AddPolicy(DevOps, p => p.RequireRole("DevOps"));
    }
  }
}
