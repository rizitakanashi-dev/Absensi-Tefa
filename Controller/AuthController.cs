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
            g.MapPost("/register-admin", async (AuthServices services, AdminOTD data) =>
            {
                try
                {
                    var isRegistered = await services.IsRegistered();
                    if (isRegistered)
                    {
                        return Results.BadRequest(new { message = "Registrasi admin ditutup karena admin sudah ada" });
                    }

                    var result = await services.AdminRegister(data);

                    return result 
                        ? Results.Ok(new { message = "Admin berhasil didaftarkan" }) 
                        : Results.BadRequest(new { message = "Gagal mendaftarkan admin" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[REGISTER ERROR]: {ex.Message}");
                    return Results.BadRequest(new { message = "Terjadi kesalahan saat registrasi" });
                }
            });

            // 2. LOGIN
            g.MapPost("/login", async (AuthServices services, IJWTService jwtServices, Login login) =>
            {
              try
              {
                  if (login == null || string.IsNullOrWhiteSpace(login.nama) || string.IsNullOrWhiteSpace(login.password))
                  {
                      return Results.BadRequest(new { message = "Nama dan password wajib diisi" });
                  }

                  var user = await services.Login(login);
                  if (user == null)
                  {
                    return Results.Unauthorized();
                  }

                  // Verifikasi password secara langsung menggunakan BCrypt.Net
                  bool isPasswordValid = false;
                  // Hanya fallback ke perbandingan plaintext jika password di DB
                  // memang bukan hash BCrypt (legacy row), bukan untuk menutupi error.
                  if (user.password.StartsWith("$2", StringComparison.Ordinal))
                  {
                      isPasswordValid = BCrypt.Net.BCrypt.Verify(login.password, user.password);
                  }
                  else
                  {
                      isPasswordValid = (login.password == user.password);
                  }

                  if (!isPasswordValid)
                  {
                      return Results.Unauthorized();
                  }

                  var userForJwt = new User
                  {
                      id = user.id,
                      Nama = user.nama,
                      Role = user.role
                  };

                  var token = jwtServices.GenerateToken(userForJwt);
                  var refreshToken = jwtServices.GenerateRefreshToken();

                  await services.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(20), user.id);

                  return Results.Ok(new LoginResponse
                    {
                      Token = token,
                      Refresh_Token = refreshToken,
                      Nama = user.nama,
                      Role = user.role
                    });
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"[LOGIN EXCEPTION]: {ex.Message}");
                  return Results.BadRequest(new { message = "Terjadi kesalahan saat login" });
              }
            });

            // 3. REFRESH TOKEN
            g.MapPost("/refresh", async (AuthServices services, RefreshRequest req, IJWTService jwtService) =>
            {
                try
                {
                    var user = await services.RefreshTokenService(req);
                    if (user == null || user.refreshTokenExpired < DateTime.UtcNow)
                    {
                        return Results.Unauthorized();
                    }

                    var userForJwt = new User
                    {
                        id = user.id,
                        Nama = user.nama,
                        Role = user.role
                    };

                    var newToken = jwtService.GenerateToken(userForJwt);
                    var newRefreshToken = jwtService.GenerateRefreshToken();

                    await services.UpdateRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(20), user.id);

                    return Results.Ok(new LoginResponse
                    {
                        Token = newToken,
                        Refresh_Token = newRefreshToken,
                        Nama = user.nama,
                        Role = user.role
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[REFRESH ERROR]: {ex.Message}");
                    return Results.BadRequest(new { message = "Terjadi kesalahan saat refresh token" });
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
                catch (Exception ex)
                {
                    Console.WriteLine($"[ME ERROR]: {ex.Message}");
                    return Results.BadRequest(new { message = "Terjadi kesalahan saat mengambil profil" });
                }
            }).RequireAuthorization();
        }
    }
}
