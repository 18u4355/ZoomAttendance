using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    [ApiController]
    [Route("api/hr")]
    public class HrController : ControllerBase
    {
        private readonly IHrRepository _hrRepo;

        public HrController(IHrRepository hrRepo)
        {
            _hrRepo = hrRepo;
        }

        /// <summary>
        /// Admin sends an invitation email to a new HR user.
        /// The user row is created (inactive) and a setup link is emailed.
        /// </summary>
        [HttpPost("invite")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> InviteHr([FromBody] InviteHrRequest request)
        {
            var result = await _hrRepo.InviteHrAsync(request);

            if (!result.IsSuccessful)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Called when the invited HR user clicks the email link and submits their password.
        /// Activates their account — no authentication required (public endpoint).
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