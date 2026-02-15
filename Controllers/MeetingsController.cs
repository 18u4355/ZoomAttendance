using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZoomAttendance.Repositories.Interfaces;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

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
    [Authorize(Roles = "HR")]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateMeetingRequest request)
    {
        var hrId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _meetingRepo.CreateMeetingAsync(request, hrId);
        return StatusCode(result.IsSuccessful ? 200 : 400, result);
    }

    [Authorize(Roles = "HR")]
    [HttpGet]
    public async Task<IActionResult> GetAllMeetings(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string status = null,
    [FromQuery] string search = null)
    {
        var result = await _meetingRepo.GetAllMeetingsAsync(page, pageSize, status, search);
        return Ok(result);
    }

    [HttpGet("{meetingId}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> GetMeetingById([FromRoute] int meetingId)
    {
        var result = await _meetingRepo.GetMeetingByIdAsync(meetingId);
        return StatusCode(result.IsSuccessful ? 200 : 404, result);
    }

    [HttpGet("dashboard/summary")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var result = await _meetingRepo.GetDashboardSummaryAsync();
        return Ok(result);
    }
    [HttpGet("{meetingId}/export")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> ExportAttendance(int meetingId)
    {
        var fileBytes = await _meetingRepo.ExportAttendanceAsync(meetingId);

        if (fileBytes == null)
            return NotFound(new
            {
                isSuccessful = false,
                message = "Meeting not found or no attendance records available"
            });

        return File(
            fileBytes,
            "text/csv",
            $"Meeting_{meetingId}_Attendance_{DateTime.UtcNow:yyyyMMdd}.csv"
        );
    }
}
