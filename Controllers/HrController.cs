using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    /// <summary>
    /// Supports inviting HR users into the system and completing their first-time account setup.
    /// </summary>
    [ApiController]
    [Route("api/hr")]
    public class HrController : ControllerBase
    {
        private readonly IHrRepository _hrRepo;
        private readonly IDepartmentRepository _departmentRepo;

        public HrController(IHrRepository hrRepo, IDepartmentRepository departmentRepo)
        {
            _hrRepo = hrRepo;
            _departmentRepo = departmentRepo;
        }

        /// <summary>
        /// Sends an HR invitation email and creates an inactive user account pending setup completion.
        /// </summary>
        [HttpPost("invite")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> InviteHr([FromBody] InviteHrRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Department))
            {
                var departments = await _departmentRepo.GetAllAsync("active", 1, 1000);
                var departmentExists = departments.Any(d =>
                    string.Equals(d.Name?.Trim(), request.Department.Trim(), StringComparison.OrdinalIgnoreCase));

                if (!departmentExists)
                    return BadRequest(Models.ResponseModels.ApiResponse<string>.Fail("The selected department does not exist."));
            }

            var result = await _hrRepo.InviteHrAsync(request);

            if (!result.IsSuccessful)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Completes the invited HR user's onboarding by setting credentials and activating the account.
        /// </summary>
        [HttpPost("complete-setup")]
        [AllowAnonymous]
        public async Task<IActionResult> CompleteSetup([FromBody] CompleteHrSetupRequest request)
        {
            var result = await _hrRepo.CompleteHrSetupAsync(request);

            if (!result.IsSuccessful)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
