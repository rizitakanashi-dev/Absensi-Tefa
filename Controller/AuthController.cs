using Absensi.Models;
using Absensi.Services;

namespace Absensi.Controller
{
    public static class AuthController
    {
        public static void MapAuth(this WebApplication app)
        {
            var g = app.MapGroup("/api/v1/auth");

            g.MapPost("/admin-register", async (AuthServices services, AdminOTD data, IPasswordService pServices) =>
            {
                try
                {
                    var isRegistered = await services.IsRegistered();
                    if (isRegistered)
                    {
                      return Results.BadRequest(new {message = "registrasi admin ditutup karena admin sudah terdaftar"});
                    }

                    data.password = pServices.HashPassword(data.password);
                    var result = await services.AdminRegister(data);

                    return result
                      ? Results.Ok(new {message = "Admin berhasil didaftarkan"})
                      : Results.BadRequest(new {message = "Gagal mendaftarkan admin"});
                } catch (Exception e)
                {
                  return Results.Problem(statusCode: 500, detail: e.Message);
                }
            });  

            g.MapPost("/login", async (AuthServices services, IPasswordService pServices, IJWTService jwtServices, Login login) =>
            {
                try 
                {
                  var user = await services.Login(login);
                  if (user == null)
                  {
                    return Results.Unauthorized();
                  }

                  if (!pServices.VerifyPassword(login.password, user.password))
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
                    Nama = user.nama,
                    Role = user.role
                  });
                } 
                catch (Exception e)
                {
                  return Results.Problem(detail: e.Message, statusCode: 500);
                }
            });
        }
    }
}
