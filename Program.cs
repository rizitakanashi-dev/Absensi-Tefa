using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics;
using System.Data; // 1. Tambahkan ini untuk IDbConnection
using Absensi.Services;
using Absensi.Controller; // Pastikan namespace sesuai dengan controller-mu

var builder = WebApplication.CreateBuilder(args);
Env.Value = builder.Configuration;

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSingleton<Database>();

// 2. Tambahkan baris ini agar IDbConnection bisa di-resolve oleh Dapper/Service
builder.Services.AddScoped<IDbConnection>(sp =>
    sp.GetRequiredService<Database>().connect());

builder.Services.AddControllers();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJWTService, JWTService>();
builder.Services.AddScoped<RoleServices>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<StatusService>();
builder.Services.AddScoped<AuthServices>();
builder.Services.AddScoped<DivisiService>();
builder.Services.AddScoped<AnggotaService>();
builder.Services.AddScoped<GuruService>();
builder.Services.AddScoped<PMService>();
builder.Services.AddScoped<AbsensiService>();

// Konfigurasi Authentication dengan Skema JwtBearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "SUPER_SECRET_KEY_KAMU_MINIMAL_32_KARAKTER"))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Logger Middleware
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

app.UseAuthentication();
app.UseAuthorization();

app.MapDivisi();
app.MapProject();
app.MapRole();
app.MapStatus();
app.MapAuth();
app.MapControllers();
app.MapAbsensiEndpoints();

app.Run();
// Radot was here
