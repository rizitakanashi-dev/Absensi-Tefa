using System.Net;
using System.Text.Json;
using Absensi.Models;
using MySql.Data.MySqlClient;

namespace Absensi.Middlewares
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var errorResponse = exception switch
            {
                UnauthorizedAccessException _ => new ErrorResponse
                {
                    ErrorCode = ErrorCodes.AUTH_UNAUTHORIZED,
                    Message = "Akses tidak diizinkan",
                    Details = exception.Message
                },
                MySqlException mysqlEx when mysqlEx.Number == 1062 => new ErrorResponse
                {
                    ErrorCode = ErrorCodes.RESOURCE_ALREADY_EXISTS,
                    Message = "Data sudah ada",
                    Details = "Terjadi duplikasi data"
                },
                MySqlException mysqlEx => new ErrorResponse
                {
                    ErrorCode = ErrorCodes.DB_CONNECTION_ERROR,
                    Message = "Kesalahan koneksi database",
                    Details = $"MySQL Error {mysqlEx.Number}"
                },
                ArgumentNullException _ => new ErrorResponse
                {
                    ErrorCode = ErrorCodes.VALIDATION_REQUIRED_FIELD,
                    Message = "Parameter wajib tidak boleh kosong",
                    Details = exception.Message
                },
                FormatException _ => new ErrorResponse
                {
                    ErrorCode = ErrorCodes.VALIDATION_INVALID_FORMAT,
                    Message = "Format data tidak valid",
                    Details = exception.Message
                },
                _ => new ErrorResponse
                {
                    ErrorCode = ErrorCodes.SERVER_INTERNAL_ERROR,
                    Message = "Terjadi kesalahan internal server",
                    Details = exception.Message
                }
            };

            context.Response.StatusCode = exception switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                ArgumentNullException => (int)HttpStatusCode.BadRequest,
                FormatException => (int)HttpStatusCode.BadRequest,
                MySqlException mysqlEx when mysqlEx.Number == 1062 => (int)HttpStatusCode.Conflict,
                MySqlException => (int)HttpStatusCode.ServiceUnavailable,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse);
            return context.Response.WriteAsync(jsonResponse);
        }
    }

    public static class GlobalExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandler>();
        }
    }
}
