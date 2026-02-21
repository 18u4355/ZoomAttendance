using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
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

        // ── ONLINE FLOW ───────────────────────────────────────────────────────

        [HttpPost("generate-link")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GenerateJoinLink(int meetingId, string email)
        {
            var result = await _attendanceRepo.GenerateJoinTokenAsync(meetingId, email);
            if (!result.IsSuccessful) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("join")]
        public async Task<IActionResult> JoinMeeting([FromQuery] string token)
        {
            var result = await _attendanceRepo.ValidateAndConfirmAsync(token);
            if (!result.IsSuccessful) return BadRequest(result.Message);
            return Redirect(result.Data);
        }

        [HttpGet("confirm")]
        public async Task<IActionResult> Confirm([FromQuery] string token)
        {
            var confirmResult = await _attendanceRepo.ConfirmCloseMeetingAsync(token);
            return Redirect(confirmResult.RedirectUrl);
        }

        // ── PHYSICAL FLOW ─────────────────────────────────────────────────────

        [HttpPost("send-qrcodes")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> SendQrCodes([FromBody] SendQrCodeRequest request)
        {
            var result = await _attendanceRepo.SendQrCodesAsync(request);
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }

        [HttpPost("scan")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Scan([FromBody] ScanAttendanceRequest request)
        {
            var result = await _attendanceRepo.ScanAsync(request);
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }

        [HttpGet("physical/{meetingId}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetPhysicalAttendance(int meetingId)
        {
            var result = await _attendanceRepo.GetMeetingAttendanceAsync(meetingId);
            return StatusCode(result.IsSuccessful ? 200 : 404, result);
        }
    }
}