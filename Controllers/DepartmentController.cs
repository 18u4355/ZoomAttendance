// Controllers/DepartmentsController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    /// <summary>
    /// Manages department records used to group staff and target meeting audiences.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/v1/departments")]
    [Produces("application/json")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentRepository _repo;

        public DepartmentsController(IDepartmentRepository repo)
        {
            _repo = repo;
        }

        // GET api/v1/departments?includeInactive=false
        /// <summary>
        /// Retrieves departments with optional status filtering and pagination.
        /// </summary>
        /// <param name="status">Optional department status filter. Expected values are <c>active</c> or <c>inactive</c>.</param>
        /// <param name="pageNumber">The page number to return.</param>
        /// <param name="pageSize">The number of records to return per page.</param>
        /// <returns>A paginated list of departments.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll(
    [FromQuery] string? status,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
        {
            try
            {
                // Validate status
                if (!string.IsNullOrEmpty(status))
                {
                    status = status.ToLower();
                    if (status != "active" && status != "inactive")
                        return BadRequest(ApiResponse<string>.Fail("Status must be 'active' or 'inactive'"));
                }

                // Validate pagination
                if (pageNumber <= 0 || pageSize <= 0)
                    return BadRequest(ApiResponse<string>.Fail("PageNumber and PageSize must be greater than 0"));

                var data = await _repo.GetAllAsync(status, pageNumber, pageSize);

                return Ok(ApiResponse<object>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // GET api/v1/departments/{id}
        /// <summary>
        /// Retrieves the details of a single department by its identifier.
        /// </summary>
        /// <param name="id">The internal department identifier.</param>
        /// <returns>The matching department record when found.</returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _repo.GetByIdAsync(id);
                if (data == null)
                    return NotFound(ApiResponse<string>.Fail($"Department with id '{id}' was not found."));
                return Ok(ApiResponse<DepartmentResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // POST api/v1/departments
        /// <summary>
        /// Creates a new department that can later be assigned to staff and meetings.
        /// </summary>
        /// <param name="request">The department payload containing the name and other creation values.</param>
        /// <returns>The created department record.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed."));

                var data = await _repo.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = data.Id },
                    ApiResponse<DepartmentResponse>.Success(data, "Department created successfully."));
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

        // PUT api/v1/departments/{id}
        /// <summary>
        /// Updates an existing department.
        /// </summary>
        /// <param name="id">The identifier of the department to update.</param>
        /// <param name="request">The updated department values.</param>
        /// <returns>The updated department record.</returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed."));

                var data = await _repo.UpdateAsync(id, request);
                return Ok(ApiResponse<DepartmentResponse>.Success(data, "Department updated successfully."));
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
        /// Deactivates a department so it is no longer available for normal operational use.
        /// </summary>
        /// <param name="id">The identifier of the department to deactivate.</param>
        /// <returns>A success message when the department is deactivated.</returns>
        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                await _repo.DeactivateAsync(id);
                return Ok(ApiResponse<string>.Success(null, "Department deactivated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<string>.Fail(ex.Message));
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

        // PATCH api/v1/departments/{id}/restore
        /// <summary>
        /// Reactivates a previously deactivated department.
        /// </summary>
        /// <param name="id">The identifier of the department to reactivate.</param>
        /// <returns>The restored department record.</returns>
        [HttpPatch("{id:int}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var data = await _repo.ActivateAsync(id);
                return Ok(ApiResponse<DepartmentResponse>.Success(data, "Department activated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<string>.Fail(ex.Message));
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

        // GET api/v1/departments/export?includeInactive=false
        /// <summary>
        /// Exports department records to Excel.
        /// </summary>
        /// <param name="includeInactive">When true, inactive departments are included in the export.</param>
        /// <returns>An Excel file containing department data.</returns>
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] bool includeInactive = false)
        {
            try
            {
                var fileBytes = await _repo.ExportAsync(includeInactive);
                var fileName = $"Departments_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
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
        /// Retrieves the meeting summary for a single department.
        /// </summary>
        /// <param name="id">The identifier of the department whose meeting summary is required.</param>
        /// <returns>A department meeting summary response.</returns>
        [HttpGet("{id:int}/meeting-summary")]
        public async Task<IActionResult> GetMeetingSummary(int id)
        {
            try
            {
                var data = await _repo.GetMeetingSummaryAsync(id);
                if (data == null)
                    return NotFound(ApiResponse<string>.Fail($"Department with id '{id}' was not found."));

                return Ok(ApiResponse<DepartmentMeetingSummaryResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }
    }
}
