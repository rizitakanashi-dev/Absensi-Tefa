using Absensi.Models;
using Absensi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Absensi.Controller
{
    public static class GuruController
    {
        public static void MapGuru(this WebApplication app)
        {
            var g = app.MapGroup("/api/v1/guru");

            // 1. GET ALL GURU (Bisa diakses oleh semua user yang sudah login)
            g.MapGet("/", async (GuruService service) =>
            {
                try
                {
                    var result = await service.GetAll();
                    return Results.Ok(result);
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { message = "Gagal mengambil data guru" });
                }
            }).RequireAuthorization();

            // 2. GET GURU BY ID (Bisa diakses oleh semua user yang sudah login)
            g.MapGet("/{id:int}", async (GuruService service, int id) =>
            {
                try
                {
                    var guru = await service.GetById(id);
                    if (guru == null)
                    {
                        return Results.NotFound(new { message = "Data guru tidak ditemukan" });
                    }

                    return Results.Ok(guru);
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { message = "Gagal mengambil detail guru" });
                }
            }).RequireAuthorization();

            // 3. CREATE GURU (Hanya Admin)
            g.MapPost("/", async (GuruService service, [FromBody] UserOTD data, IPasswordService pServices) =>
            {
                try
                {
                    data.password = pServices.HashPassword(data.password);
                    var result = await service.Create(data);

                    return result 
                        ? Results.Ok(new { message = "Data guru berhasil ditambahkan" }) 
                        : Results.BadRequest(new { message = "Gagal menambahkan data guru" });
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { message = "Gagal memproses penambahan guru" });
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin"));

            // 4. UPDATE GURU (Hanya Admin)
            g.MapPut("/{id:int}", async (GuruService service, int id, [FromBody] UserOTD data) =>
            {
                try
                {
                    var isUpdated = await service.Update(id, data);
                    return isUpdated 
                        ? Results.Ok(new { message = "Data guru berhasil diperbarui" }) 
                        : Results.NotFound(new { message = "Data guru tidak ditemukan atau gagal diperbarui" });
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { message = "Gagal memperbarui data guru" });
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin"));

            // 5. DELETE GURU (Hanya Admin)
            g.MapDelete("/{id:int}", async (GuruService service, int id) =>
            {
                try
                {
                    var isDeleted = await service.Delete(id);
                    return isDeleted 
                        ? Results.Ok(new { message = "Data guru berhasil dihapus" }) 
                        : Results.NotFound(new { message = "Data guru tidak ditemukan" });
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { message = "Gagal menghapus data guru" });
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        }
    }
}
