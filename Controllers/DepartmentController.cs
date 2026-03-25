// Controllers/DepartmentsController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
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
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            try
            {
                var data = await _repo.GetAllAsync(includeInactive);
                return Ok(ApiResponse<object>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // GET api/v1/departments/{id}
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

        // DELETE api/v1/departments/{id}  →  soft delete
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _repo.DeleteAsync(id);
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
        [HttpPatch("{id:int}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            try
            {
                var data = await _repo.RestoreAsync(id);
                return Ok(ApiResponse<DepartmentResponse>.Success(data, "Department restored successfully."));
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
    }
}