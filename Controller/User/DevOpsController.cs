using Absensi.Models;
using Absensi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Absensi.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class DevOpsController : ControllerBase
    {
        private readonly DevOpsService _devOpsService;

        public DevOpsController(DevOpsService devOpsService)
        {
            _devOpsService = devOpsService;
        }

        // GET: api/DevOps
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _devOpsService.GetAll();
            return Ok(data);
        }

        // GET: api/DevOps/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _devOpsService.GetById(id);
            if (data == null)
            {
                return NotFound(new { message = "DevOps user tidak ditemukan" });
            }
            return Ok(data);
        }

        // POST: api/DevOps/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserOTD data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.nama) || string.IsNullOrWhiteSpace(data.password))
            {
                return BadRequest(new { message = "Nama dan password tidak boleh kosong" });
            }

            var isSuccess = await _devOpsService.Register(data);

            if (isSuccess)
            {
                return Ok(new { message = "DevOps user berhasil ditambahkan" });
            }

            return BadRequest(new { message = "Gagal menambahkan DevOps user" });
        }

        // PUT: api/DevOps/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserOTD data)
        {
            if (data == null)
            {
                return BadRequest(new { message = "Data tidak boleh kosong" });
            }

            var isUpdated = await _devOpsService.Update(id, data);

            if (isUpdated)
            {
                return Ok(new { message = "DevOps user berhasil diperbarui" });
            }

            return NotFound(new { message = "DevOps user tidak ditemukan" });
        }

        // DELETE: api/DevOps/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var isDeleted = await _devOpsService.Delete(id);

            if (isDeleted)
            {
                return Ok(new { message = "DevOps user berhasil dihapus" });
            }

            return NotFound(new { message = "DevOps user tidak ditemukan" });
        }
    }
}
