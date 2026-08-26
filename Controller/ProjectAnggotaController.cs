using absensi.models;
using absensi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Absensi.Controllers
{
    [ApiController]
    [Route("api/v1/project-anggota")]
    [Authorize]
    public class ProjectAnggotaController : ControllerBase
    {
        private readonly ProjectAnggotaService _service;

        public ProjectAnggotaController(ProjectAnggotaService service)
        {
            _service = service;
        }

        // GET: api/v1/project-anggota
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAll();
            return Ok(data);
        }

        // POST: api/v1/project-anggota
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] projectanggotadto req)
        {
            var result = await _service.Create(req);
            if (!result)
            {
                return BadRequest(new { message = "Gagal menambahkan anggota ke project" });
            }

            return Ok(new { message = "Berhasil menambahkan anggota ke project" });
        }

        // DELETE: api/v1/project-anggota/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.Delete(id);
            if (!result)
            {
                return NotFound(new { message = "Data tidak ditemukan atau gagal dihapus" });
            }

            return Ok(new { message = "Berhasil menghapus anggota dari project" });
        }
    }
}
