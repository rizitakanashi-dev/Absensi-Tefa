using System.Text;
using System.Diagnostics;
using Absensi.Services;
using Absensi.Controller;
var builder = WebApplication.CreateBuilder(args);
Env.Value = builder.Configuration;
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<Database>();

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<RoleServices>();
builder.Services.AddScoped<StatusService>();
builder.Services.AddScoped<AuthServices>();
builder.Services.AddScoped<DivisiService>();

var app = builder.Build();
//Logger
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        throw;
    }
    finally
    {
        sw.Stop();

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        Console.WriteLine(
            $"INFO: {ip} - \"{context.Request.Method} {context.Request.Path} {context.Response.StatusCode}\" {sw.ElapsedMilliseconds}ms"
        );
    }
});
// Configure the HTTP request pipeline.
app.MapDivisi();
app.MapRole();
app.MapStatus();
app.MapAuth();
app.Run();


