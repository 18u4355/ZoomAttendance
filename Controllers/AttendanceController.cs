using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceRepository _attendanceRepo;

        public AttendanceController(IAttendanceRepository attendanceRepo)
        {
            _attendanceRepo = attendanceRepo;
        }

        // 🔑 Generate join token and send email
        // Public endpoint for HR to invite staff
        [HttpPost("generate-link")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GenerateLink([FromQuery] int meetingId, [FromQuery] string email)
        {
            var response = await _attendanceRepo.GenerateJoinTokenAsync(meetingId, email);

            if (!response.IsSuccessful)
                return BadRequest(response);

            return Ok(response);
        }

        // 📝 Staff joins meeting using token
        [HttpPost("join")]
        [AllowAnonymous] // No JWT required
        public async Task<IActionResult> Join([FromQuery] string token)
        {
            var response = await _attendanceRepo.LogAttendanceAsync(token);

            if (!response.IsSuccessful)
                return BadRequest(response);

            return Ok(response);
        }

        // ✅ Staff confirms attendance
        [HttpPost("confirm")]
        [AllowAnonymous] // No JWT required
        public async Task<IActionResult> Confirm([FromQuery] string token)
        {
            var response = await _attendanceRepo.ConfirmAttendanceAsync(token);

            if (!response.IsSuccessful)
                return BadRequest(response);

            return Ok(response);
        }

        // 📊 HR: Get attendance report between dates
        [HttpGet("report")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Report([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var response = await _attendanceRepo.GetAttendanceReportAsync(start, end);

            if (!response.IsSuccessful)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
