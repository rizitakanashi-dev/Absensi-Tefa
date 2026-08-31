using Absensi.Models;
using Absensi.Services;
using System.Security.Claims;

namespace Absensi.Controller
{
    public static class AbsensiController
    {
        public static void MapAbsensiEndpoints(this WebApplication app)
        {
            var g = app.MapGroup("/api/v1/absen").RequireAuthorization();

            g.MapGet("/", async (string tanggal, AbsensiService service) =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(tanggal))
                            return Results.BadRequest(new { message = "parameter tanggal wajib diisi" });

                        var result = await service.GetRekapByTanggal(tanggal);
                        return Results.Ok(result);
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(new { message = e.Message });
                    }
                    
    
                });

            g.MapPost("/masuk", async (AbsenMasukDTO req, ClaimsPrincipal user, AbsensiService service) =>
                {
                    try
                    {
                        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (string.IsNullOrEmpty(userIdClaim))
                            return Results.BadRequest(new { message = "token user tidak valid" });
                        int idUser = int.Parse(userIdClaim);
                        bool success = await service.AbsenMasuk(idUser, req);

                        if (!success)
                            return Results.BadRequest(new { message = "gagal melakukan absen masuk, periksa kembali id project" });

                        return Results.Ok(new { message = "absen masuk berhasil" });
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(new { message = e.Message });
                    }
                });

            g.MapPut("/pulang", async (AbsenPulangDTO req, AbsensiService service) =>
              {
                  try
                  {
                      bool success = await service.AbsenPulang(req);

                      if (!success)
                          return Results.BadRequest(new { message = "gagal melakukan absen pulang, data absensi tidak ditemukan" });

                      return Results.Ok(new { message = "Absen pulang berhasil" });
                  }
                  catch (Exception e)
                  {
                      return Results.BadRequest(new { message = e.Message });
                  }
              });
        }
    }
}
