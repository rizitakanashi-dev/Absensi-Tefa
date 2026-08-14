using Absensi.Models;
using Absensi.Services;

namespace Absensi.Controller
{
    public static class AuthController
    {
        public static void MapAuth(this WebApplication app)
        {
            var g = app.MapGroup("/api/v1/auth");
            
            g.MapPost("/admin-register",async(AuthServices services,AdminOTD data,IPasswordService pServices) => {
            try
            {
                data.password = pServices.HashPassword(data.password);
                var isRegistered = await services.IsRegistered();
                if(!isRegistered){
                  return Results.Unauthorized();
                }

                var result = await services.AdminRegister(data);
                if(!result){
                return Results.BadRequest();
                }
                return Results.Ok();
            }
            catch (Exception e)
            {
                  return Results.InternalServerError(e.Message);
            } 
                });
        }
    }
}
