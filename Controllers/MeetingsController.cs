using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    [ApiController]
    [Route("api/meetings")]
    [Authorize(Roles = "HR")]
    public class MeetingsController : ControllerBase
    {
        private readonly IMeetingRepository _meetingRepo;
        private readonly IAttendanceRepository _attendanceRepo;

        public MeetingsController(IMeetingRepository meetingRepo, IAttendanceRepository attendanceRepo)
        {
            _meetingRepo = meetingRepo;
            _attendanceRepo = attendanceRepo;
        }

        // ── ONLINE FLOW ───────────────────────────────────────────────────────

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateMeetingRequest request)
        {
            var hrId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _meetingRepo.CreateMeetingAsync(request, hrId);
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMeetings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string status = null,
            [FromQuery] string search = null)
        {
            var result = await _meetingRepo.GetAllMeetingsAsync(page, pageSize, status, search);
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }

        [HttpGet("{meetingId}")]
        public async Task<IActionResult> GetMeetingById([FromRoute] int meetingId)
        {
            var result = await _meetingRepo.GetMeetingByIdAsync(meetingId);
            return StatusCode(result.IsSuccessful ? 200 : 404, result);
        }

        [HttpPost("close/{meetingId}")]
        public async Task<IActionResult> CloseMeeting([FromRoute] int meetingId)
        {
            var result = await _attendanceRepo.CloseMeetingAsync(meetingId);
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }

        [HttpGet("dashboard/summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var result = await _meetingRepo.GetDashboardSummaryAsync();
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }

        [HttpGet("{meetingId}/attendance")]
        public async Task<IActionResult> GetMeetingAttendance([FromRoute] int meetingId)
        {
            var result = await _meetingRepo.GetMeetingAttendanceAsync(meetingId);
            return StatusCode(result.IsSuccessful ? 200 : 404, result);
        }

        [HttpGet("{meetingId}/export")]
        public async Task<IActionResult> ExportAttendance([FromRoute] int meetingId)
        {
            var fileBytes = await _meetingRepo.ExportAttendanceAsync(meetingId);
            if (fileBytes == null)
                return NotFound(new { isSuccessful = false, message = "Meeting not found or no attendance records available" });

            return File(
                fileBytes,
                "text/csv",
                $"Meeting_{meetingId}_Attendance_{DateTime.UtcNow:yyyyMMdd}.csv"
            );
        }

        [Authorize(Roles = "HR")]
        [HttpPost("{meetingId}/send-invites")]
        public async Task<IActionResult> SendInvites(
int meetingId,
[FromBody] SendMeetingInviteRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            var result = await _meetingRepo
                .SendMeetingInvitesAsync(meetingId, request);

            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }

        // ── PHYSICAL FLOW ─────────────────────────────────────────────────────

        [HttpGet("staff/emails")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetAllStaffEmails()
        {
            var result = await _meetingRepo.GetAllStaffEmailsAsync();
            return StatusCode(result.IsSuccessful ? 200 : 400, result);
        }

        [HttpGet("{meetingId}/physical/summary")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetPhysicalSummary([FromRoute] int meetingId)
        {
            var result = await _meetingRepo.GetMeetingPhysicalSummaryAsync(meetingId);
            return StatusCode(result.IsSuccessful ? 200 : 404, result);
        }

        [HttpGet("{meetingId}/physical/export")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> ExportPhysicalAttendance([FromRoute] int meetingId)
        {
            var fileBytes = await _meetingRepo.ExportPhysicalAttendanceAsync(meetingId);
            if (fileBytes == null)
                return NotFound(new { isSuccessful = false, message = "Meeting not found or no physical attendance records available" });

            return File(
                fileBytes,
                "text/csv",
                $"Meeting_{meetingId}_PhysicalAttendance_{DateTime.UtcNow:yyyyMMdd}.csv"
            );
        }
    }
}