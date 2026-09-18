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

            g.MapGet("/", async (string tanggal, int? page, int? pageSize, AbsensiService service) =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(tanggal))
                            return Results.BadRequest(new { message = "parameter tanggal wajib diisi" });

                        // Validasi format tanggal untuk mencegah SQL injection
                        if (!DateTime.TryParseExact(tanggal, "yyyy-MM-dd", null, 
                            System.Globalization.DateTimeStyles.None, out _))
                        {
                            return Results.BadRequest(new { message = "Format tanggal harus yyyy-MM-dd" });
                        }

                        // Default pagination values
                        int currentPage = page ?? 1;
                        int size = pageSize ?? 50;

                        if (currentPage < 1) currentPage = 1;
                        if (size < 1 || size > 100) size = 50; // Max 100 per page

                        var result = await service.GetRekapByTanggal(tanggal, currentPage, size);
                        var totalCount = await service.GetRekapCount(tanggal);

                        return Results.Ok(new
                        {
                            data = result,
                            pagination = new
                            {
                                page = currentPage,
                                pageSize = size,
                                totalRecords = totalCount,
                                totalPages = (int)Math.Ceiling((double)totalCount / size)
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"ABSEN GET: {e.Message}");
                        return Results.BadRequest(new { message = "Gagal mengambil rekap absen" });
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
                        Console.WriteLine($"ABSEN MASUK: {e.Message}");
                        return Results.BadRequest(new { message = "Gagal melakukan absen masuk" });
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
                      Console.WriteLine($"ABSEN PULANG: {e.Message}");
                      return Results.BadRequest(new { message = "Gagal melakukan absen pulang" });
                  }
              });
        }
    }
}
