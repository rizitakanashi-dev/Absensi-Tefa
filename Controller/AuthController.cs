using Absensi.Models;
using Absensi.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Absensi.Controller
{
    public static class AuthController
    {
        public static void MapAuth(this WebApplication app)
        {
            var g = app.MapGroup("/api/v1/auth");

            // 1. REGISTER ADMIN
            g.MapPost("/register-admin", async (AuthServices services, AdminOTD data, IPasswordService pServices) =>
            {
                try
                {
                    var isRegistered = await services.IsRegistered();
                    if (isRegistered)
                    {
                        return Results.BadRequest(new { message = "Registrasi admin ditutup karena admin sudah ada" });
                    }

                    data.password = pServices.HashPassword(data.password);
                    var result = await services.AdminRegister(data);

                    return result 
                        ? Results.Ok(new { message = "Admin berhasil didaftarkan" }) 
                        : Results.BadRequest(new { message = "Gagal mendaftarkan admin" });
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { message = "Gagal memproses pendaftaran admin" });
                }
            });

            // 2. LOGIN
            g.MapPost("/login", async (AuthServices services, IPasswordService pServices, IJWTService jwtServices, [FromBody] Login login) =>
            {
                try
                {
                    var user = await services.Login(login);
                    if (user == null)
                    {
                        return Results.Unauthorized();
                    }

                    if (!pServices.VerifyPassword(login.password, user.Password))
                    {
                        return Results.Unauthorized();
                    }

                    var token = jwtServices.GenerateToken(user);
                    var refreshToken = jwtServices.GenerateRefreshToken();

                    await services.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(20), user.id);

                    return Results.Ok(new LoginResponse
                    {
                        Token = token,
                        Refresh_Token = refreshToken,
                        Nama = user.Nama,
                        Role = user.Role
                    });
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { message = "Gagal memproses login" });
                }
            });

            // 3. REFRESH TOKEN
            g.MapPost("/refresh", async (AuthServices services, [FromBody] RefreshRequest req, IJWTService jwtService) =>
            {
                try
                {
                    var user = await services.RefreshTokenService(req);
                    if (user == null || user.refreshTokenExpired < DateTime.UtcNow)
                    {
                        return Results.Unauthorized();
                    }

                    var newToken = jwtService.GenerateToken(user);
                    var newRefreshToken = jwtService.GenerateRefreshToken();

                    await services.UpdateRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(20), user.id);

                    return Results.Ok(new LoginResponse
                    {
                        Token = newToken,
                        Refresh_Token = newRefreshToken,
                        Nama = user.Nama,
                        Role = user.Role
                    });
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { message = "Gagal memperbarui token" });
                }
            });

            // 4. GET ME (PROFIL USER LOGIN)
            g.MapGet("/me", async (AuthServices services, HttpContext httpContext) =>
            {
                try
                {
                    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!int.TryParse(userIdClaim, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var user = await services.GetMe(userId);
                    if (user == null)
                    {
                        return Results.NotFound(new { message = "User tidak ditemukan" });
                    }

                    return Results.Ok(user);
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { message = "Gagal mengambil data profil" });
                }
            }).RequireAuthorization();
        }
    }
}
