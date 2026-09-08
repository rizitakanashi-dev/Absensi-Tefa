using Absensi.Models;
using Absensi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Absensi.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnggotaController : ControllerBase
    {
        private readonly AnggotaService _anggotaService;
        private readonly IPasswordService _passwordService;

        public AnggotaController(AnggotaService anggotaService, IPasswordService passwordService)
        {
            _anggotaService = anggotaService;
            _passwordService = passwordService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserOTD data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.nama) || string.IsNullOrWhiteSpace(data.password))
            {
                return BadRequest(new { message = "Data Nama dan Password Tidak Boleh Kosong!" });
            }

            // Hash password sebelum dikirim ke database
            data.password = _passwordService.HashPassword(data.password);

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
