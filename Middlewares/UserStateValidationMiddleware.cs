using Absensi.Services;

namespace Absensi.Middlewares
{
    /// <summary>
    /// Middleware validasi state user terhadap database.
    /// Skepped untuk request tanpa autentikasi (login, register, health,
    /// docs) agar endpoint publik tetap berfungsi.
    /// </summary>
    public class UserStateValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public UserStateValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ActiveUserService activeUserService)
        {
            // Lewati request yang tidak terautentikasi (endpoint publik)
            // dan request yang belum sampai ke UseAuthentication.
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            if (!context.User.TryGetUserId(out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Token tidak valid." });
                return;
            }

            var tokenRole = context.User.GetRoleName();

            if (!await activeUserService.IsStillActiveAsync(userId, tokenRole, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Sesi tidak valid. Silakan login kembali." });
                return;
            }

            await _next(context);
        }
    }

    public static class UserStateValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserStateValidation(this IApplicationBuilder app)
        {
            return app.UseMiddleware<UserStateValidationMiddleware>();
        }
    }
}