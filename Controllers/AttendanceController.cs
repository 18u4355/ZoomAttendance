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

        [HttpPost("generate-link")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GenerateJoinLink(int meetingId, string email)
        {
            var result = await _attendanceRepo
                .GenerateJoinTokenAsync(meetingId, email);

            if (!result.IsSuccessful)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("join")]
        public async Task<IActionResult> JoinMeeting([FromQuery] string token)
        {
            var result = await _attendanceRepo
                .ValidateAndConfirmAsync(token);

            if (!result.IsSuccessful)
                return BadRequest(result.Message);
            return Redirect(result.Data);
        }


        [Authorize(Roles = "HR")]
        [HttpPost("/api/meetings/close/{meetingId}")]
        public async Task<IActionResult> CloseMeeting([FromRoute] int meetingId)
        {
            var result = await _attendanceRepo.CloseMeetingAsync(meetingId);
            return StatusCode(result.IsSuccessful ? 200 : 400, result);

        }
        [HttpGet("confirm")]
        public async Task<IActionResult> Confirm([FromQuery] string token)
        {
            var (_, redirectUrl) = await _attendanceRepo.ConfirmCloseMeetingAsync(token);
            return Redirect(redirectUrl);
        }
    }
}