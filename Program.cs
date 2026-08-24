using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

 // 1. Konfigurasi Authentication dengan Skema JwtBearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, // Sesuaikan dengan konfigurasi JWTService kamu
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "SUPER_SECRET_KEY_KAMU_MINIMAL_32_KARAKTER"))
    };
});

builder.Services.AddAuthorization();

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
app.UseAuthentication();
// Configure the HTTP request pipeline.
app.UseAuthorization();
app.MapDivisi();
app.MapProject();
app.MapRole();
app.MapStatus();
app.MapAuth();
app.MapControllers();
app.Run();

// Radot was here
