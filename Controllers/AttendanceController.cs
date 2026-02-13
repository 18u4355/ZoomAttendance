using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Implementations;
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

        // Generate join token and send email
        // Public endpoint for HR to invite staff
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
        // Staff joins meeting using token
        [HttpGet("join")]
        public async Task<IActionResult> JoinMeeting([FromQuery] string token)
        {
            var result = await _attendanceRepo
                .ValidateAndConfirmAsync(token);

            if (!result.IsSuccessful)
                return BadRequest(result.Message);

            // 🔥 Direct redirect to Zoom
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
    //        [Authorize(Roles = "HR")]
    //        [HttpGet("report")]
    //        public async Task<IActionResult> GetAttendanceReport([FromQuery] AttendanceReportRequest request)
    //        {
    //            var result = await _attendanceRepo.GetAttendanceReportAsync(request);

    //            if (!result.IsSuccessful)
    //                return BadRequest(result);

    //            // Optional: CSV export
    //            if (request.ExportCsv && result.Data != null)
    //            {
    //                var csv = new StringBuilder();
    //                csv.AppendLine("MeetingTitle,StaffName,JoinTime,ConfirmedAttendance,ConfirmationTime");
    //                foreach (var item in result.Data.Items)
    //                {
    //                    csv.AppendLine($"{item.MeetingTitle},{item.StaffName},{item.JoinTime:yyyy-MM-dd HH:mm},{item.ConfirmedAttendance},{item.ConfirmationTime:yyyy-MM-dd HH:mm}");
    //                }
    //                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
    //                return File(bytes, "text/csv", "AttendanceReport.csv");
    //            }

    //            return Ok(result);
    //        }

}


