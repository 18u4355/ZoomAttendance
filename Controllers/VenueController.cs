using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/venues")]
    [Produces("application/json")]
    public class VenuesController : ControllerBase
    {
        private readonly IVenueRepository _repo;

        public VenuesController(IVenueRepository repo)
        {
            _repo = repo;
        }

        // GET api/v1/venues
        // GET api/v1/venues?includeInactive=true
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

        // GET api/v1/venues/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _repo.GetByIdAsync(id);
                if (data == null)
                    return NotFound(ApiResponse<string>.Fail($"Venue with id '{id}' was not found."));
                return Ok(ApiResponse<VenueResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // POST api/v1/venues
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVenueRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed."));

                var data = await _repo.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = data.Id },
                    ApiResponse<VenueResponse>.Success(data, "Venue created successfully."));
            }
            catch (InvalidOperationException ex)
            {
                var msg = ex.Message;
                if (msg.StartsWith("DUPLICATE:"))
                    return Conflict(ApiResponse<string>.Fail(msg.Split(':', 2)[1]));
                return BadRequest(ApiResponse<string>.Fail(msg));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // PUT api/v1/venues/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateVenueRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ApiResponse<string>.Fail("Validation failed."));

                var data = await _repo.UpdateAsync(id, request);
                return Ok(ApiResponse<VenueResponse>.Success(data, "Venue updated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<string>.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                var msg = ex.Message;
                if (msg.StartsWith("DUPLICATE:"))
                    return Conflict(ApiResponse<string>.Fail(msg.Split(':', 2)[1]));
                return BadRequest(ApiResponse<string>.Fail(msg));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }

        // DELETE api/v1/venues/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _repo.DeleteAsync(id);
                return Ok(ApiResponse<string>.Success(null, "Venue deactivated successfully."));
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
