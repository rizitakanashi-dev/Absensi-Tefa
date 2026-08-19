using Absensi.Models;
using Absensi.Services;

namespace Absensi.Controller
{
    public static class StatusController
    {
        public static void MapStatus(this WebApplication app)
        {
            var g = app.MapGroup("/api/v1/status");

            g.MapGet("/", async (StatusService services) =>
                {
                    try
                    {
                        var data = await services.Get();
                        return Results.Ok(data);
                    }
                    catch (Exception e)
                    {
                        return Results.InternalServerError(e.Message);
                    }
                });
        }
    }
}
