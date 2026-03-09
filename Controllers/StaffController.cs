// Controllers/StaffController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    [ApiController]
    [Route("api/v1/staff")]
    [Produces("application/json")]
    [Authorize]
    public class StaffController : ControllerBase
    {
        private readonly IStaffRepository _repo;

        public StaffController(IStaffRepository repo)
        {
            _repo = repo;
        }

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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStaffRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.ToString()));

                var created = await _repo.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id },
                    ApiResponse<StaffResponse>.Success(created, "Staff member created successfully."));
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

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStaffRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.ToString()));

                var updated = await _repo.UpdateAsync(id, request);
                return Ok(ApiResponse<StaffResponse>.Success(updated, "Staff member updated successfully."));
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

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStaffStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.ToString()));

                await _repo.UpdateStatusAsync(id, request);
                return Ok(ApiResponse<string>.Success("Staff status updated successfully."));
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _repo.DeleteAsync(id);
                return Ok(ApiResponse<string>.Success("Staff member deleted successfully."));
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
        // Add these two endpoints to Controllers/StaffController.cs
        // Also add this using if not present:
        //   using Microsoft.AspNetCore.Http;

        // GET api/v1/staff/upload-template
        [HttpGet("upload-template")]
        public async Task<IActionResult> GetUploadTemplate()
        {
            try
            {
                var fileBytes = await _repo.GetUploadTemplateAsync();
                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "StaffUploadTemplate.xlsx"
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("Failed to generate template.", ex.Message));
            }
        }

        // POST api/v1/staff/bulk-upload
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
    }
}