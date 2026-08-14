using Absensi.Services;
using Absensi.Models;

namespace Absensi.Controller
{
    public static class RoleController
    {
        public static void MapRole(this WebApplication app)
        {
            var g = app.MapGroup("/api/v1/role");

            g.MapGet("/", async (RoleServices services) =>
            {
                try
                {
                    var data_services = await services.Get();
                    return Results.Ok(data_services);
                }
                catch (Exception e)
                {
                    return Results.InternalServerError(e.Message);
                }
            });
        }
    }
}
