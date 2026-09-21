using Absensi.Models;
using Absensi.Services;
using System.Security.Claims;

namespace Absensi.Controller
{
    public static class HostingRequestController
    {
        public static void MapHostingRequest(this WebApplication app)
        {
            var g = app.MapGroup("/api/v1/hosting").RequireAuthorization();

            // 1. GET All Requests (dengan filter)
            g.MapGet("/requests", async (string? status, HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                    IEnumerable<HostingRequestListDTO> result;

                    // Hanya Admin, PM, dan DevOps yang boleh melihat seluruh request.
                    if (userRole is "Admin" or "PM" or "DevOps")
                    {
                        result = await service.GetAll(status);
                    }
                    else if (userRole == "Anggota")
                    {
                        result = await service.GetAll(status, userId);
                    }
                    else
                    {
                        return Results.Forbid();
                    }

                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING GET ALL: {e.Message}");
                    return Results.Problem("Gagal mengambil data hosting request");
                }
            });

            // 2. GET My Requests (shortcut untuk anggota)
            g.MapGet("/my-requests", async (HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var result = await service.GetAll(null, userId);
                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING GET MY: {e.Message}");
                    return Results.Problem("Gagal mengambil data hosting request");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "PM", "DevOps", "Anggota"));

            // 3. GET Pending Requests (untuk PM)
            g.MapGet("/pending", async (HostingRequestService service) =>
            {
                try
                {
                    var result = await service.GetAll(HostingStatus.Pending);
                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING GET PENDING: {e.Message}");
                    return Results.Problem("Gagal mengambil data pending request");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "PM"));

            // 4. GET Approved Requests (untuk DevOps)
            g.MapGet("/approved", async (HostingRequestService service) =>
            {
                try
                {
                    var result = await service.GetAll(HostingStatus.Approved);
                    return Results.Ok(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING GET APPROVED: {e.Message}");
                    return Results.Problem("Gagal mengambil data approved request");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "DevOps"));

            // 5. GET Request by ID
            g.MapGet("/request/{id:int}", async (int id, HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var request = await service.GetById(id);
                    if (request == null)
                        return Results.NotFound(new { message = "Hosting request tidak ditemukan" });

                    // Authorization check: Anggota hanya bisa lihat request sendiri
                    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                    if (userRole == "Anggota" && request.IdUser != userId)
                        return Results.Forbid();

                    if (userRole is not ("Admin" or "PM" or "DevOps" or "Anggota"))
                        return Results.Forbid();

                    return Results.Ok(request);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING GET BY ID: {e.Message}");
                    return Results.Problem("Gagal mengambil detail hosting request");
                }
            });

            // 6. POST Create Request
            g.MapPost("/request", async (HostingRequestCreateDTO data, HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var newId = await service.Create(userId, data);
                    return Results.Created($"/api/v1/hosting/request/{newId}", new { id = newId, message = "Hosting request berhasil dibuat" });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING POST: {e.Message}");
                    return Results.Problem("Gagal membuat hosting request");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Anggota"));

            // 7. PUT Update Request (hanya jika pending atau rejected)
            g.MapPut("/request/{id:int}", async (int id, HostingRequestUpdateDTO data, HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    // Check ownership
                    var existing = await service.GetById(id);
                    if (existing == null)
                        return Results.NotFound(new { message = "Hosting request tidak ditemukan" });

                    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

                    // Hanya owner atau admin yang bisa update
                    if (userRole != "Admin" && existing.IdUser != userId)
                        return Results.Forbid();

                    // Cek status (hanya pending/rejected yang bisa diupdate)
                    if (existing.Status != HostingStatus.Pending && existing.Status != HostingStatus.Rejected)
                        return Results.BadRequest(new { message = "Request tidak bisa diupdate karena sudah diproses" });

                    var isUpdated = await service.Update(id, data);
                    return isUpdated
                        ? Results.Ok(new { message = "Hosting request berhasil diperbarui" })
                        : Results.BadRequest(new { message = "Gagal memperbarui hosting request" });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING PUT: {e.Message}");
                    return Results.Problem("Gagal memperbarui hosting request");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "Anggota"));

            // 8. PUT Approve Request (PM)
            g.MapPut("/request/{id:int}/approve", async (int id, HostingRequestReviewDTO data, HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var pmId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var isApproved = await service.Approve(id, pmId, data.Notes);
                    
                    return isApproved
                        ? Results.Ok(new { message = "Hosting request berhasil di-approve" })
                        : Results.BadRequest(new { message = "Gagal approve request. Pastikan status masih pending." });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING APPROVE: {e.Message}");
                    return Results.Problem("Gagal approve hosting request");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "PM"));

            // 9. PUT Reject Request (PM)
            g.MapPut("/request/{id:int}/reject", async (int id, HostingRequestReviewDTO data, HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var pmId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var isRejected = await service.Reject(id, pmId, data.Notes);
                    
                    return isRejected
                        ? Results.Ok(new { message = "Hosting request berhasil di-reject" })
                        : Results.BadRequest(new { message = "Gagal reject request. Pastikan status masih pending." });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING REJECT: {e.Message}");
                    return Results.Problem("Gagal reject hosting request");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "PM"));

            // 10. PUT Start Processing (DevOps)
            g.MapPut("/request/{id:int}/start", async (int id, HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var devopsId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var isStarted = await service.StartProcessing(id, devopsId, user.IsInRole("Admin"));
                    
                    return isStarted
                        ? Results.Ok(new { message = "Hosting request mulai diproses" })
                        : Results.BadRequest(new { message = "Gagal start processing. Pastikan status approved." });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING START: {e.Message}");
                    return Results.Problem("Gagal start processing");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "DevOps"));

            // 11. PUT Complete Hosting (DevOps)
            g.MapPut("/request/{id:int}/complete", async (int id, HostingRequestDevOpsUpdateDTO data, HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var devopsId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var isAdmin = user.IsInRole("Admin");
                    var isCompleted = await service.Complete(id, data, devopsId, isAdmin);
                    
                    return isCompleted
                        ? Results.Ok(new { message = "Hosting request selesai" })
                        : Results.BadRequest(new { message = "Gagal complete. Pastikan status in_progress." });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING COMPLETE: {e.Message}");
                    return Results.Problem("Gagal complete hosting");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "DevOps"));

            // 12. PUT Update DevOps Notes
            g.MapPut("/request/{id:int}/notes", async (int id, HostingRequestReviewDTO data, HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    var devopsId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var isAdmin = user.IsInRole("Admin");
                    var isUpdated = await service.UpdateDevOpsNotes(id, data.Notes, devopsId, isAdmin);
                    
                    return isUpdated
                        ? Results.Ok(new { message = "DevOps notes berhasil diupdate" })
                        : Results.BadRequest(new { message = "Gagal update notes" });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING UPDATE NOTES: {e.Message}");
                    return Results.Problem("Gagal update notes");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "DevOps"));

            // 13. DELETE Cancel Request (soft delete)
            g.MapDelete("/request/{id:int}/cancel", async (int id, HostingRequestService service, ClaimsPrincipal user) =>
            {
                try
                {
                    // Check ownership
                    var existing = await service.GetById(id);
                    if (existing == null)
                        return Results.NotFound(new { message = "Hosting request tidak ditemukan" });

                    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

                    // Hanya owner atau admin yang bisa cancel
                    if (userRole != "Admin" && existing.IdUser != userId)
                        return Results.Forbid();

                    var isCancelled = await service.Cancel(id);
                    return isCancelled
                        ? Results.Ok(new { message = "Hosting request berhasil dibatalkan" })
                        : Results.BadRequest(new { message = "Gagal cancel. Hanya pending/rejected yang bisa dibatalkan." });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING CANCEL: {e.Message}");
                    return Results.Problem("Gagal cancel hosting request");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin", "Anggota"));

            // 14. DELETE Hard Delete (Admin only)
            g.MapDelete("/request/{id:int}", async (int id, HostingRequestService service) =>
            {
                try
                {
                    var isDeleted = await service.Delete(id);
                    return isDeleted
                        ? Results.Ok(new { message = "Hosting request berhasil dihapus" })
                        : Results.NotFound(new { message = "Hosting request tidak ditemukan" });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"HOSTING DELETE: {e.Message}");
                    return Results.Problem("Gagal menghapus hosting request");
                }
            }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        }
    }
}
