using Absensi.Services;
using Absensi.Models;

namespace Absensi.Controller
{
    public static class DivisiController
    {
      public static void MapDivisi(this WebApplication app)
      {
        var g = app.MapGroup("/api/v1/divisi");

        g.MapGet("/", async (DivisiService services) => {
          try
          {
            var data = await services.Get();
            return Results.Ok(data);
          } catch (Exception e){
            return Results.InternalServerError(e.Message);
          }
        });

        g.MapGet("/{id:int}", async (int id, DivisiService services) => {
          try{
            var data = await services.GetById(id);         
            if (data == null) return Results.NotFound("Divisi tidak ditemukan");
            return Results.Ok(data);
          } catch (Exception e) {
            return Results.InternalServerError(e.Message);
          }
        });

        g.MapPost("/", async (DivisiDTO dto, DivisiService services) => {
            try {
              var rowsAffected = await services.Create(dto);
              return Results.Created($"/api/v1/role", dto);
            } catch (Exception e) {
              return Results.InternalServerError(e.Message);
            }
        });

        g.MapPut("/{id:int}", async (int id,DivisiDTO dto, DivisiService services) => {
            try{
              dto.id = id;
              var rowsAffected = await services.Update(dto);
              if (rowsAffected == 0) return Results.NotFound("Divisi tidak ditemukan");
              return Results.Ok("data berhasil diperbarui");
            } catch (Exception e){
              return Results.InternalServerError(e.Message);
            }
        });

        g.MapDelete("/{id:int}", async (int id, DivisiService services) => {
            try {
            var rowsAffected = await services.Delete(id);
            if (rowsAffected == 0) return Results.NotFound("Divisi tidak ditemukan");
            return Results.Ok("data berhasil di hapus");
            } catch (Exception e){
              return Results.InternalServerError(e.Message);
            }
        });
      }
    }
}
