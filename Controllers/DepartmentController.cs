// Controllers/DepartmentsController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    [ApiController]
    [Route("api/v1/departments")]
    [Produces("application/json")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentRepository _repo;

        public DepartmentsController(IDepartmentRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _repo.GetAllAsync();
                return Ok(ApiResponse<IEnumerable<DepartmentResponse>>.Success(data));
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
                    return NotFound(ApiResponse<string>.Fail($"Department with id '{id}' was not found."));

                return Ok(ApiResponse<DepartmentResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.ToString()));

                var created = await _repo.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id },
                    ApiResponse<DepartmentResponse>.Success(created, "Department created successfully."));
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
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed.", ModelState.ToString()));

                var updated = await _repo.UpdateAsync(id, request);
                return Ok(ApiResponse<DepartmentResponse>.Success(updated, "Department updated successfully."));
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
                return Ok(ApiResponse<string>.Success("Department deleted successfully."));
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