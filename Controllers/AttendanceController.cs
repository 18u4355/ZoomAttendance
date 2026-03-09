// Controllers/AttendanceController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    [ApiController]
    [Route("api/v1/attendance")]
    [Produces("application/json")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceRepository _repo;

        public AttendanceController(IAttendanceRepository repo)
        {
            _repo = repo;
        }

        // GET api/v1/attendance/{meetingId}
        [HttpGet("{meetingId:int}")]
        public async Task<IActionResult> GetByMeeting(int meetingId, [FromQuery] AttendanceFilterRequest filter)
        {
            try
            {
                var data = await _repo.GetAttendanceAsync(meetingId, filter);
                return Ok(ApiResponse<PagedAttendanceResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // GET api/v1/attendance/{meetingId}/summary
        [HttpGet("{meetingId:int}/summary")]
        public async Task<IActionResult> GetSummary(int meetingId)
        {
            try
            {
                var data = await _repo.GetSummaryAsync(meetingId);
                return Ok(ApiResponse<AttendanceSummaryResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // GET api/v1/attendance/{meetingId}/export
        [HttpGet("{meetingId:int}/export")]
        public async Task<IActionResult> Export(int meetingId, [FromQuery] AttendanceFilterRequest filter)
        {
            try
            {
                var fileBytes = await _repo.ExportAsync(meetingId, filter);
                var fileName = $"Attendance_{meetingId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // POST api/v1/attendance/confirm-end
        [AllowAnonymous]
        [HttpPost("confirm-end")]
        public async Task<IActionResult> VirtualEndConfirm([FromBody] VirtualEndConfirmRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed."));

                var result = await _repo.VirtualEndConfirmAsync(request.Token);
                return Ok(ApiResponse<CheckInResponse>.Success(result, result.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<string>.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // PATCH api/v1/attendance/status
        [HttpPatch("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] ManualStatusUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed."));

                await _repo.UpdateStatusAsync(request.MeetingId, request.StaffId, request.Status);
                return Ok(ApiResponse<string>.Success(null, "Attendance status updated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<string>.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }
    }
}