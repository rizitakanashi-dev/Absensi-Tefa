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
            g.MapGet("/", async (int? userId, int? projectId, int? statusId, 
                TargetService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

                    // Jika user biasa (Anggota), hanya bisa lihat target sendiri
                    if (userRole == "Anggota")
                    {
                        userId = currentUserId;
                    }

                    var result = await service.GetAll(userId, projectId, statusId);
                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"TARGET GET: {e.Message}");
                    return Results.Problem("Gagal mengambil data target");
                }
            });

            // 2. GET Target by ID
            g.MapGet("/{id:int}", async (int id, TargetService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var target = await service.GetById(id);
                    if (target == null)
                        return Results.NotFound(new { message = "Target tidak ditemukan" });

                    // Check authorization: user hanya bisa lihat target sendiri
                    var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

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

            // 3. GET My Targets (untuk user melihat target mereka sendiri)
            g.MapGet("/my", async (TargetService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var result = await service.GetByUserId(userId);
                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"TARGET GET MY: {e.Message}");
                    return Results.Problem("Gagal mengambil data target");
                }
            });

            // 4. POST Create Target (Admin/PM/Guru)
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

            // 5. PUT Update Target
            g.MapPut("/{id:int}", async (int id, TargetUpdateDTO data, 
                TargetService service, ClaimsPrincipal user) =>
            {
                try
                {
                    // Check jika target exists
                    var existing = await service.GetById(id);
                    if (existing == null)
                        return Results.NotFound(new { message = "Target tidak ditemukan" });

                    // Authorization: user hanya bisa update target sendiri (kecuali Admin/PM/Guru)
                    var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

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

            // 6. DELETE Target (Hanya Admin)
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
