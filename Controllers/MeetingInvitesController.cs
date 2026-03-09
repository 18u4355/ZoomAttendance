// Controllers/MeetingInvitesController.cs
// SaveLocation endpoint removed

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    [ApiController]
    [Route("api/v1/meeting-invites")]
    [Produces("application/json")]
    [Authorize]
    public class MeetingInvitesController : ControllerBase
    {
        private readonly IMeetingInviteRepository _inviteRepo;
        private readonly IAttendanceRepository _attendanceRepo;

        public MeetingInvitesController(
            IMeetingInviteRepository inviteRepo,
            IAttendanceRepository attendanceRepo)
        {
            _inviteRepo = inviteRepo;
            _attendanceRepo = attendanceRepo;
        }

        // GET api/v1/meeting-invites/{meetingId}/emails-preview
        [HttpGet("{meetingId}/emails-preview")]
        public async Task<IActionResult> GetEmailsPreview(int meetingId)
        {
            try
            {
                var data = await _inviteRepo.GetEmailsPreviewAsync(meetingId);
                return Ok(ApiResponse<object>.Success(data));
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

        // POST api/v1/meeting-invites/{meetingId}/send
        [HttpPost("{meetingId}/send")]
        public async Task<IActionResult> SendInvites(int meetingId)
        {
            try
            {
                var result = await _inviteRepo.SendInvitesAsync(meetingId);
                return Ok(ApiResponse<SendInvitesResponse>.Success(result, result.Message));
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

        // POST api/v1/meeting-invites/{meetingId}/resend/{staffId}
        [HttpPost("{meetingId}/resend/{staffId}")]
        public async Task<IActionResult> ResendInvite(int meetingId, int staffId)
        {
            try
            {
                await _inviteRepo.ResendInviteAsync(meetingId, staffId);
                return Ok(ApiResponse<string>.Success(null, "Invite resent successfully."));
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

        // GET api/v1/meeting-invites/{meetingId}
        [HttpGet("{meetingId}")]
        public async Task<IActionResult> GetInvites(int meetingId)
        {
            try
            {
                var data = await _inviteRepo.GetInvitesByMeetingAsync(meetingId);
                return Ok(ApiResponse<object>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // POST api/v1/meeting-invites/attendance/confirm
        [AllowAnonymous]
        [HttpPost("attendance/confirm")]
        public async Task<IActionResult> PhysicalCheckIn([FromBody] PhysicalCheckInRequest request)
        {
            try
            {
                var result = await _attendanceRepo.PhysicalCheckInAsync(
                    request.Token,
                    request.Latitude,
                    request.Longitude);
                return Ok(ApiResponse<CheckInResponse>.Success(result));
            }
            catch (InvalidOperationException ex)
            {
                var msg = ex.Message;
                if (msg.StartsWith("OUTSIDE_FENCE:"))
                    return BadRequest(ApiResponse<string>.Fail("You are not within the meeting location range."));
                if (msg.StartsWith("ALREADY_CHECKED_IN:"))
                    return Conflict(ApiResponse<string>.Fail("You have already checked in."));
                if (msg.StartsWith("NOT_FOUND:"))
                    return NotFound(ApiResponse<string>.Fail("Attendance record not found."));
                return BadRequest(ApiResponse<string>.Fail(msg));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // POST api/v1/meeting-invites/attendance/join
        [AllowAnonymous]
        [HttpPost("attendance/join")]
        public async Task<IActionResult> VirtualJoin([FromBody] VirtualJoinRequest request)
        {
            try
            {
                var result = await _attendanceRepo.VirtualJoinAsync(request.Token);
                return Ok(ApiResponse<VirtualJoinResponse>.Success(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }
    }
}