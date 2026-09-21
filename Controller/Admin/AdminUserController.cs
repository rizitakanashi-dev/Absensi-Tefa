using Absensi.Models;
using Absensi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Absensi.Controller
{
    [ApiController]
    [Route("api/v1/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUserController : ControllerBase
    {
        private readonly AdminUserService service;

        public AdminUserController(AdminUserService service)
        {
            this.service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? roleId)
        {
            try
            {
                return Ok(await service.GetAll(roleId));
            }
            catch (Exception)
            {
                return Problem("Gagal mengambil data user.");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var user = await service.GetById(id);
                return user is null
                    ? NotFound(new { message = "User tidak ditemukan." })
                    : Ok(user);
            }
            catch (Exception)
            {
                return Problem("Gagal mengambil detail user.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdminUserCreateDTO data)
        {
            if (data is null || string.IsNullOrWhiteSpace(data.Nama) || string.IsNullOrWhiteSpace(data.Password))
                return BadRequest(new { message = "Nama, password, dan role wajib diisi." });

            try
            {
                var id = await service.Create(data);
                return CreatedAtAction(nameof(GetById), new { id }, new { id, message = "User berhasil dibuat." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] AdminUserUpdateDTO data)
        {
            if (data is null || string.IsNullOrWhiteSpace(data.Nama))
                return BadRequest(new { message = "Nama wajib diisi." });

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
                return Unauthorized();

            try
            {
                var updated = await service.Update(actorId, id, data);
                return updated
                    ? Ok(new { message = "User berhasil diperbarui." })
                    : NotFound(new { message = "User tidak ditemukan." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
                return Unauthorized();

            try
            {
                var deleted = await service.Delete(actorId, id);
                return deleted
                    ? Ok(new { message = "User berhasil dihapus." })
                    : NotFound(new { message = "User tidak ditemukan." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
