using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics;
using System.Data;
using Absensi.Services;
using Absensi.Controller;
using Absensi.Models;

var builder = WebApplication.CreateBuilder(args);
Env.Value = builder.Configuration;


builder.Services.AddOpenApi();
builder.Services.AddSingleton<Database>();

builder.Services.AddScoped<IDbConnection>(sp =>
    sp.GetRequiredService<Database>().connect());

builder.Services.AddAuthorization(Policies.Register);

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
builder.Services.AddScoped<ProjectAnggotaService>();

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

builder.Services.AddCors(options =>
    {
      options.AddPolicy("AllowFrontend", policy =>
          {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
          });
    });

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

app.UseCors("AllowFrontend");

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
// Rizi was here
