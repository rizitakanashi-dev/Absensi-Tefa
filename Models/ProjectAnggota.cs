namespace absensi.models
{
  public class projectanggota
  {
    public int id {get; set;}
    public int iduser {get; set;}
    public int idproject {get; set;}

    public string username {get; set;} = string.Empty;
    public string project {get; set;} = string.Empty;
  }

  public class projectanggotaotd
  {
    public int id_user {get; set;}
    public int id_project {get; set;}
  }

  public class projectanggotadto
  {
    public int user {get; set;}
    public int project {get; set;} 
  }
}
