// Controllers/MeetingInvitesController.cs
// ResendInvite staffId changed to Guid

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    /// <summary>
    /// Handles invitation preview, sending, resending, and attendance entry points reached from invitation links.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v1/meeting-invites")]
    [Produces("application/json")]
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
        /// <summary>
        /// Returns the list of staff members who are scheduled to receive invitation emails for a meeting.
        /// </summary>
        /// <param name="meetingId">The meeting whose invitation recipients should be previewed.</param>
        /// <returns>A preview list of recipients and their departments.</returns>
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
        /// <summary>
        /// Sends meeting invitations to the resolved recipients for the specified meeting.
        /// </summary>
        /// <param name="meetingId">The meeting whose invitations should be sent.</param>
        /// <returns>A send summary containing total, sent, and failed counts.</returns>
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
        /// <summary>
        /// Resends a meeting invitation to a single staff member.
        /// </summary>
        /// <param name="meetingId">The meeting identifier.</param>
        /// <param name="staffId">The staff member who should receive the resend.</param>
        /// <returns>A success message when the resend completes.</returns>
        [HttpPost("{meetingId}/resend/{staffId:guid}")]
        public async Task<IActionResult> ResendInvite(int meetingId, Guid staffId)
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
        /// <summary>
        /// Retrieves the stored invitation records for a meeting.
        /// </summary>
        /// <param name="meetingId">The meeting identifier.</param>
        /// <returns>The invitation history for the meeting.</returns>
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
        /// <summary>
        /// Confirms physical attendance by validating the invite token and the attendee's GPS coordinates.
        /// </summary>
        /// <param name="request">The attendance confirmation payload containing the token and current coordinates.</param>
        /// <returns>The resulting attendance status after fence validation.</returns>
        [AllowAnonymous]
        [HttpPost("attendance/confirm")]
        public async Task<IActionResult> PhysicalCheckIn([FromBody] PhysicalCheckInRequest request)
        {
            try
            {
                var result = await _attendanceRepo.PhysicalCheckInAsync(
                    request.Token, request.Latitude, request.Longitude);
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
        /// <summary>
        /// Resolves a virtual meeting invite token and returns the Zoom join URL.
        /// </summary>
        /// <param name="request">The token payload from a virtual meeting invitation.</param>
        /// <returns>The Zoom join URL that should be opened by the client.</returns>
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

        /// <summary>
        /// Redirects a browser directly to the Zoom join URL for a virtual invitation token.
        /// </summary>
        /// <param name="token">The invitation token supplied in the query string.</param>
        /// <returns>An HTTP redirect response to the Zoom join URL.</returns>
        [AllowAnonymous]
        [HttpGet("attendance/join")]
        public async Task<IActionResult> VirtualJoinLink([FromQuery] string token)
        {
            try
            {
                var result = await _attendanceRepo.VirtualJoinAsync(token);

                // redirect user to Zoom
                return Redirect(result.ZoomJoinUrl);
            }
            catch (Exception ex)
            {
                var safeMessage = HtmlEncoder.Default.Encode(ex.Message);
                return Content($"<h3>{safeMessage}</h3>", "text/html");
            }
        }
    }
}
