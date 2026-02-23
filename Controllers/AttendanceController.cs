using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using ZoomAttendance.Data;
using ZoomAttendance.Models;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Repositories.Implementations;
using ZoomAttendance.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ZoomAttendance.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly ApplicationDbContext _context;
        public AttendanceController(IAttendanceRepository attendanceRepo, ApplicationDbContext context)
        {
            _attendanceRepo = attendanceRepo;
            _context = context;
        }


        [HttpPost("generate-link")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GenerateJoinLink(int meetingId, string email)
        {
            var result = await _attendanceRepo
                .GenerateAndSendLinkAsync(meetingId, email);

            if (!result.IsSuccessful)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("join")]
        public async Task<IActionResult> JoinMeeting([FromQuery] string token)
        {
            var result = await _attendanceRepo.ValidateAndConfirmAsync(token);
            if (!result.IsSuccessful) return BadRequest(result.Message);
            return Redirect(result.Data);
        }

        [Authorize(Roles = "HR")]
        [HttpPost("close/{meetingId}")]
        public async Task<IActionResult> CloseMeeting([FromRoute] int meetingId)
        {
            var result = await _attendanceRepo.CloseMeetingAsync(meetingId);
            return StatusCode(result.IsSuccessful ? 200 : 400, result);

        }

        [HttpGet("confirm")]
        public async Task<IActionResult> Confirm([FromQuery] string token)
        {
            var confirmResult = await _attendanceRepo.ConfirmCloseMeetingAsync(token);
            return Redirect(confirmResult.RedirectUrl);
        }

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

        [HttpGet("staff-emails")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetAllStaffEmails()
        {
            var result = await _attendanceRepo.GetAllStaffEmailsAsync();
            return StatusCode(result.IsSuccessful ? 200 : 404, result);
        }


    }
}