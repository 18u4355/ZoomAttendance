// Controllers/DashboardController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    /// <summary>
    /// Exposes dashboard data used to power the main administrative overview screen.
    /// </summary>
    [ApiController]
    [Route("api/v1/dashboard")]
    [Produces("application/json")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardRepository _repo;

        public DashboardController(IDashboardRepository repo)
        {
            _repo = repo;
        }

        // GET api/v1/dashboard
        /// <summary>
        /// Retrieves the dashboard snapshot including counts, attendance metrics, and upcoming meetings.
        /// </summary>
        /// <returns>A dashboard response containing high-level operational statistics.</returns>
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var data = await _repo.GetStatsAsync();
                return Ok(ApiResponse<DashboardResponse>.Success(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred.", ex.Message));
            }
        }
    }
}
