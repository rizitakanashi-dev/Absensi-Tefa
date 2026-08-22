using Absensi.Models;
using Absensi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Absensi.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PMController : ControllerBase
    {
        private readonly PMService _pmService;

        public PMController(PMService pmService)
        {
            _pmService = pmService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserOTD data)
        {
            if (data == null)
            {
                return BadRequest(new { message = "Data Tidak Boleh Kosong!" });
            }

            var isSuccess = await _pmService.Register(data);

            if (isSuccess)
            {
                return Ok(new { message = "Project Manager berhasil ditambahkan" });
            }

            return BadRequest(new { message = "Gagal mendaftarkan Project Manager" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _pmService.GetAll();
            return Ok(data);
        }
    }
}
