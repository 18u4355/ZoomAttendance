// Controllers/StaffController.cs
// Id changed to Guid

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    /// <summary>
    /// Manages staff records, status changes, bulk import, templates, and exports.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v1/staff")]
    [Produces("application/json")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffRepository _repo;

        public StaffController(IStaffRepository repo)
        {
            _repo = repo;
        }

        // GET api/v1/staff
        /// <summary>
        /// Retrieves paginated staff records using the supplied filters.
        /// </summary>
        /// <param name="filter">Search, department, status, and pagination filters for staff retrieval.</param>
        /// <returns>A paginated list of staff members.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] StaffFilterRequest filter)
        {
            try
            {
                var data = await _repo.GetAllAsync(filter);
                return Ok(ApiResponse<PagedStaffResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // GET api/v1/staff/{id}
        /// <summary>
        /// Retrieves a single staff record by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the staff member.</param>
        /// <returns>The requested staff record when found.</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var data = await _repo.GetByIdAsync(id);
                if (data == null)
                    return NotFound(ApiResponse<string>.Fail($"Staff with id '{id}' was not found."));
                return Ok(ApiResponse<StaffResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // POST api/v1/staff
        /// <summary>
        /// Creates a new staff member record.
        /// </summary>
        /// <param name="request">The staff creation payload.</param>
        /// <returns>The created staff record.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStaffRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed."));

                var data = await _repo.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = data.Id },
                    ApiResponse<StaffResponse>.Success(data, "Staff created successfully."));
            }
            catch (InvalidOperationException ex)
            {
                var msg = ex.Message;
                if (msg.StartsWith("DUPLICATE:"))
                    return Conflict(ApiResponse<string>.Fail(msg.Split(':', 2)[1]));
                if (msg.StartsWith("NOT_FOUND:"))
                    return NotFound(ApiResponse<string>.Fail(msg.Split(':', 2)[1]));
                return BadRequest(ApiResponse<string>.Fail(msg));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // PUT api/v1/staff/{id}
        /// <summary>
        /// Updates an existing staff member.
        /// </summary>
        /// <param name="id">The identifier of the staff member to update.</param>
        /// <param name="request">The updated staff payload.</param>
        /// <returns>The updated staff record.</returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStaffRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed."));

                var data = await _repo.UpdateAsync(id, request);
                return Ok(ApiResponse<StaffResponse>.Success(data, "Staff updated successfully."));
            }
            catch (InvalidOperationException ex)
            {
                var msg = ex.Message;
                if (msg.StartsWith("NOT_FOUND:"))
                    return NotFound(ApiResponse<string>.Fail(msg.Split(':', 2)[1]));
                if (msg.StartsWith("DUPLICATE:"))
                    return Conflict(ApiResponse<string>.Fail(msg.Split(':', 2)[1]));
                return BadRequest(ApiResponse<string>.Fail(msg));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // DELETE api/v1/staff/{id}
        /// <summary>
        /// Deletes a staff member record.
        /// </summary>
        /// <param name="id">The identifier of the staff member to delete.</param>
        /// <returns>A success message when deletion completes.</returns>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _repo.DeleteAsync(id);
                return Ok(ApiResponse<string>.Success(null, "Staff deleted successfully."));
            }
            catch (InvalidOperationException ex)
            {
                var msg = ex.Message;
                if (msg.StartsWith("NOT_FOUND:"))
                    return NotFound(ApiResponse<string>.Fail(msg.Split(':', 2)[1]));
                return BadRequest(ApiResponse<string>.Fail(msg));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // PATCH api/v1/staff/{id}/status
        /// <summary>
        /// Updates the status of a staff member, for example activating or deactivating them.
        /// </summary>
        /// <param name="id">The identifier of the staff member whose status should change.</param>
        /// <param name="request">The requested new status value.</param>
        /// <returns>A success message when the status change completes.</returns>
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStaffStatusRequest request)
        {
            try
            {
                await _repo.UpdateStatusAsync(id, request.Status);
                return Ok(ApiResponse<string>.Success(null, "Staff status updated successfully."));
            }
            catch (InvalidOperationException ex)
            {
                var msg = ex.Message;
                if (msg.StartsWith("NOT_FOUND:"))
                    return NotFound(ApiResponse<string>.Fail(msg.Split(':', 2)[1]));
                return BadRequest(ApiResponse<string>.Fail(msg));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // GET api/v1/staff/upload-template
        /// <summary>
        /// Downloads the Excel template used for bulk staff import.
        /// </summary>
        /// <returns>An Excel template file for bulk upload preparation.</returns>
        [HttpGet("upload-template")]
        public async Task<IActionResult> GetUploadTemplate()
        {
            try
            {
                var fileBytes = await _repo.GetUploadTemplateAsync();
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "StaffUploadTemplate.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("Failed to generate template.", ex.Message));
            }
        }

        // POST api/v1/staff/bulk-upload
        /// <summary>
        /// Imports staff members from an uploaded Excel file.
        /// </summary>
        /// <param name="file">The Excel file containing the staff rows to import.</param>
        /// <returns>A summary of successful and failed imports.</returns>
        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(ApiResponse<string>.Fail("No file uploaded."));
                if (Path.GetExtension(file.FileName).ToLower() != ".xlsx")
                    return BadRequest(ApiResponse<string>.Fail("Only .xlsx files are accepted."));

                var result = await _repo.BulkUploadAsync(file);
                var message = $"Upload complete. {result.Succeeded} succeeded, {result.Failed} failed.";
                return Ok(ApiResponse<BulkUploadResponse>.Success(result, message));
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

        // GET api/v1/staff/export
        /// <summary>
        /// Exports staff records to Excel using the supplied filters.
        /// </summary>
        /// <param name="filter">Optional staff filters to apply before exporting.</param>
        /// <returns>An Excel file containing the filtered staff records.</returns>
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] StaffFilterRequest filter)
        {
            try
            {
                var fileBytes = await _repo.ExportAsync(filter);
                var fileName = $"Staff_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }
    }
}
