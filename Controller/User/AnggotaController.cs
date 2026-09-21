using Absensi.Models;
using Absensi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Absensi.Controller
{
    [ApiController]
    [Route("api/[controller]")]
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
    }
}
