// Controllers/AttendanceController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    /// <summary>
    /// Provides attendance reporting and export endpoints for meetings and individual staff members.
    /// </summary>
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

        /// <summary>
        /// Retrieves paginated attendance records across meetings using the supplied filter criteria.
        /// </summary>
        /// <param name="filter">Optional search, status, date, and pagination filters for attendance records.</param>
        /// <returns>A paginated attendance result set wrapped in the standard API response format.</returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAttendance([FromQuery] AttendanceFilterRequest filter)
        {
            try
            {
                var data = await _repo.GetAttendanceAsync(filter);
                return Ok(ApiResponse<PagedAttendanceResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // GET api/v1/attendance/{meetingId}/summary
        /// <summary>
        /// Returns the attendance summary for a single meeting, including present, absent, late, joined, and checked-in counts.
        /// </summary>
        /// <param name="meetingId">The internal identifier of the meeting to summarize.</param>
        /// <returns>A summary object describing attendance totals for the meeting.</returns>
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
        /// <summary>
        /// Exports attendance records for a meeting as an Excel file.
        /// </summary>
        /// <param name="meetingId">The internal identifier of the meeting to export.</param>
        /// <param name="filter">Optional attendance filters applied before export.</param>
        /// <returns>An Excel document containing the filtered attendance rows.</returns>
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

        /// <summary>
        /// Returns a staff-centric attendance report that combines summary metrics with the staff member's meeting attendance history.
        /// </summary>
        /// <param name="staffId">The unique identifier of the staff member whose report is being requested.</param>
        /// <param name="request">Optional date filters used to narrow the reporting period.</param>
        /// <returns>A detailed attendance report for the specified staff member.</returns>
        [Authorize]
        [HttpGet("report/{staffId:guid}")]
        public async Task<IActionResult> GetStaffReport(Guid staffId, [FromQuery] StaffAttendanceReportRequest request)
        {
            try
            {
                var data = await _repo.GetStaffReportAsync(staffId, request);
                return Ok(ApiResponse<StaffAttendanceReportResponse>.Success(data));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }
    }
}
