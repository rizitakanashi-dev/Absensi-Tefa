using Absensi.Models;
using Absensi.Services;
using System.Security.Claims;

namespace Absensi.Controller
{
    public static class TargetController
    {
        public static void MapTarget(this WebApplication app)
        {
            var g = app.MapGroup("/api/v1/target").RequireAuthorization();

            // 1. GET All Targets (Admin/PM/Guru bisa filter by user, user biasa hanya bisa lihat milik sendiri)
            // Catatan: daftarkan /my SEBELUM /{id} agar tidak bentrok di beberapa host.
            g.MapGet("/my", async (TargetService service, ClaimsPrincipal user) =>
            {
                try
                {
                    if (!user.TryGetUserId(out var userId))
                        return Results.Unauthorized();

                    var result = await service.GetByUserId(userId);
                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"TARGET GET MY: {e.Message}");
                    return Results.Problem("Gagal mengambil data target");
                }
            });

            g.MapGet("/", async (int? userId, int? projectId, int? statusId,
                TargetService service, ClaimsPrincipal user) =>
            {
                try
                {
                    if (!user.TryGetUserId(out var currentUserId))
                        return Results.Unauthorized();

                    var userRole = user.GetRoleName();

                    if (userRole == "Anggota")
                        userId = currentUserId;

                    var result = await service.GetAll(userId, projectId, statusId);
                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"TARGET GET: {e.Message}");
                    return Results.Problem("Gagal mengambil data target");
                }
            });

            g.MapGet("/{id:int}", async (int id, TargetService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var target = await service.GetById(id);
                    if (target == null)
                        return Results.NotFound(new { message = "Target tidak ditemukan" });

                    if (!user.TryGetUserId(out var currentUserId))
                        return Results.Unauthorized();

                    var userRole = user.GetRoleName();

                    if (userRole == "Anggota" && target.IdUser != currentUserId)
                        return Results.Forbid();

                    return Results.Ok(target);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"TARGET GET BY ID: {e.Message}");
                    return Results.Problem("Gagal mengambil data target");
                }
            });

            g.MapPost("/", async (TargetCreateDTO data, TargetService service) =>
            {
                try
                {
                    var newId = await service.Create(data);
                    return Results.Created($"/api/v1/target/{newId}", new { id = newId, message = "Target berhasil dibuat" });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"TARGET POST: {e.Message}");
                    return Results.Problem("Gagal menambahkan target");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "PM", "Guru"));

            g.MapPut("/{id:int}", async (int id, TargetUpdateDTO data,
                TargetService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var existing = await service.GetById(id);
                    if (existing == null)
                        return Results.NotFound(new { message = "Target tidak ditemukan" });

                    if (!user.TryGetUserId(out var currentUserId))
                        return Results.Unauthorized();

                    var userRole = user.GetRoleName();

                    if (userRole == "Anggota" && existing.IdUser != currentUserId)
                        return Results.Forbid();

                    data.Id = id;
                    var isUpdated = await service.Update(data);

                    return isUpdated
                        ? Results.Ok(new { message = "Target berhasil diperbarui" })
                        : Results.BadRequest(new { message = "Tidak ada data yang diperbarui" });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"TARGET PUT: {e.Message}");
                    return Results.Problem("Gagal memperbarui target");
                }
            });

            g.MapDelete("/{id:int}", async (int id, TargetService service) =>
            {
                try
                {
                    var isDeleted = await service.Delete(id);
                    return isDeleted
                        ? Results.Ok(new { message = "Target berhasil dihapus" })
                        : Results.NotFound(new { message = "Target tidak ditemukan" });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"TARGET DELETE: {e.Message}");
                    return Results.Problem("Gagal menghapus target");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        }
    }
}
