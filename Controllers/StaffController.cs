using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "HR")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffRepository _staffRepository;

        public StaffController(IStaffRepository staffRepository)
        {
            _staffRepository = staffRepository;
        }

        // ── Physical staff (QR attendance) ────────────────────────────────────

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterStaffRequest request)
        {
            var result = await _staffRepository.RegisterAsync(request);
            return StatusCode(result.IsSuccessful ? 201 : 400, result);
        }

        [HttpGet("physical")]
        public async Task<IActionResult> GetAll([FromQuery] PaginatedStaffRequest request)
        {
            var result = await _staffRepository.GetAllAsync(request);
            return Ok(result);
        }

        [HttpGet("physical/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _staffRepository.GetByIdAsync(id);
            return StatusCode(result.IsSuccessful ? 200 : 404, result);
        }

        [HttpDelete("Physical/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _staffRepository.DeleteAsync(id);
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }
        [HttpGet("physical/history/{email}")]
        public async Task<IActionResult> GetPhysicalHistory(string email)
        {
            var result = await _staffRepository.GetPhysicalAttendanceHistoryAsync(email);
            return StatusCode(result.IsSuccessful ? 200 : 404, result);
        }

        // ── Virtual staff (Zoom attendance) ───────────────────────────────────

        [HttpPost("virtual")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
        {
            var result = await _staffRepository.CreateStaffAsync(request);
            return Ok(result);
        }

        [HttpGet("virtual")]
        public async Task<IActionResult> GetAllStaff([FromQuery] PaginatedStaffRequest request)
        {
            var result = await _staffRepository.GetAllStaffAsync(request);
            return Ok(result);
        }
        [HttpDelete("virtual/{id}")]
        public async Task<IActionResult> DeleteVirtualStaff(int id)
        {
            var result = await _staffRepository.DeleteVirtualStaffAsync(id);
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }
        [HttpGet("virtual/history/{email}")]
        public async Task<IActionResult> GetVirtualHistory(string email)
        {
            var result = await _staffRepository.GetVirtualAttendanceHistoryAsync(email);
            return StatusCode(result.IsSuccessful ? 200 : 404, result);
        }
    }
}