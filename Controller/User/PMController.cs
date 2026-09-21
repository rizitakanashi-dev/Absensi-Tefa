using Absensi.Models;
using Absensi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Absensi.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PMController : ControllerBase
    {
        private readonly PMService _pmService;

        public PMController(PMService pmService)
        {
            _pmService = pmService;
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _pmService.GetById(id);
            return data is null
                ? NotFound(new { message = "Project Manager tidak ditemukan" })
                : Ok(data);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UserOTD data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.nama))
                return BadRequest(new { message = "Nama tidak boleh kosong" });

            var updated = await _pmService.Update(id, data);
            return updated
                ? Ok(new { message = "Project Manager berhasil diperbarui" })
                : NotFound(new { message = "Project Manager tidak ditemukan" });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _pmService.Delete(id);
            return deleted
                ? Ok(new { message = "Project Manager berhasil dihapus" })
                : NotFound(new { message = "Project Manager tidak ditemukan" });
        }
    }
}
