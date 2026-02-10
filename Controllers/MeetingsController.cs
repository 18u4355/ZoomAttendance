using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZoomAttendance.Repositories.Interfaces;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

[ApiController]
[Route("api/meetings")]
[Authorize(Roles = "HR")] // Only HR creates/ends meetings
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
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var result = await _meetingRepo.GetActiveMeetingsAsync();
        return StatusCode(result.IsSuccessful ? 200 : 400, result);
    }
}

