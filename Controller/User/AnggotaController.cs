using Absensi.Models;
using Absensi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Absensi.Controller
{
    [ApiController]
    [Route("api/v1/anggota")]
    [Route("api/Anggota")]
    [Authorize]
    public class AnggotaController : ControllerBase
    {
        private readonly AnggotaService _anggotaService;

        public AnggotaController(AnggotaService anggotaService)
        {
            _anggotaService = anggotaService;
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] UserOTD data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.nama) || string.IsNullOrWhiteSpace(data.password))
            {
                return BadRequest(new { message = "Data Nama dan Password Tidak Boleh Kosong!" });
            }

            // Hash password dilakukan di dalam service (AnggotaService.Register)
            var isSuccess = await _anggotaService.Register(data);

            if (isSuccess)
            {
                return Ok(new { message = "Anggota berhasil ditambahkan" });
            }

            return BadRequest(new { message = "Gagal menambahkan anggota" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _anggotaService.GetAll();
            return Ok(data);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _anggotaService.GetById(id);
            return data is null
                ? NotFound(new { message = "Anggota tidak ditemukan" })
                : Ok(data);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UserOTD data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.nama))
                return BadRequest(new { message = "Nama tidak boleh kosong" });

            var updated = await _anggotaService.Update(id, data);
            return updated
                ? Ok(new { message = "Anggota berhasil diperbarui" })
                : NotFound(new { message = "Anggota tidak ditemukan" });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _anggotaService.Delete(id);
            return deleted
                ? Ok(new { message = "Anggota berhasil dihapus" })
                : NotFound(new { message = "Anggota tidak ditemukan" });
        }
    }
}
