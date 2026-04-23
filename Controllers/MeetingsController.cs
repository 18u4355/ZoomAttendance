// Controllers/MeetingsController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    /// <summary>
    /// Manages meetings, including creation, retrieval, updates, and deletion.
    /// </summary>
    [ApiController]
    [Route("api/v1/meetings")]
    [Produces("application/json")]
    [Authorize]
    public class MeetingsController : ControllerBase
    {
        private readonly IMeetingRepository _repo;

        public MeetingsController(IMeetingRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Retrieves paginated meetings using the supplied filters such as mode, audience type, status, and date range.
        /// </summary>
        /// <param name="filter">Optional search and pagination parameters for meeting retrieval.</param>
        /// <returns>A paginated collection of meeting records.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] MeetingFilterRequest filter)
        {
            try
            {
                var data = await _repo.GetAllAsync(filter);
                return Ok(ApiResponse<PagedMeetingResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        /// <summary>
        /// Retrieves a single meeting by its identifier, including related department information.
        /// </summary>
        /// <param name="id">The internal identifier of the meeting.</param>
        /// <returns>The requested meeting record when found.</returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _repo.GetByIdAsync(id);
                if (data == null)
                    return NotFound(ApiResponse<string>.Fail($"Meeting with id '{id}' was not found."));

                return Ok(ApiResponse<MeetingResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        /// <summary>
        /// Creates a new meeting and, where required, provisions the related Zoom meeting details.
        /// </summary>
        /// <param name="request">The meeting payload, including schedule, venue, virtual settings, and audience information.</param>
        /// <returns>The created meeting record.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMeetingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.ToString()));

                var created = await _repo.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id },
                    ApiResponse<MeetingResponse>.Success(created, "Meeting created successfully."));
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

        /// <summary>
        /// Updates an existing meeting and synchronizes the linked Zoom meeting when applicable.
        /// </summary>
        /// <param name="id">The identifier of the meeting to update.</param>
        /// <param name="request">The updated meeting payload.</param>
        /// <returns>The updated meeting record.</returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMeetingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.ToString()));

                var updated = await _repo.UpdateAsync(id, request);
                return Ok(ApiResponse<MeetingResponse>.Success(updated, "Meeting updated successfully."));
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

        /// <summary>
        /// Deletes a meeting by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the meeting to delete.</param>
        /// <returns>A success message when the deletion completes.</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _repo.DeleteAsync(id);
                return Ok(ApiResponse<string>.Success("Meeting deleted successfully."));
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
