using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZoomAttendance.Models.RequestModels;

namespace ZoomAttendance.Controllers
{
    /// <summary>
    /// Exposes authenticated HR profile and account settings endpoints.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "HR")]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsRepository _repository;

        public SettingsController(ISettingsRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Retrieves the profile and settings of the currently authenticated HR user.
        /// </summary>
        /// <returns>The current HR user's settings payload.</returns>
        [HttpGet("me")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetMySettings()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid token");

            var result = await _repository.GetMySettingsAsync(userId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Updates the profile information of the currently authenticated HR user.
        /// </summary>
        /// <param name="request">The updated profile details to persist.</param>
        /// <returns>The updated settings payload.</returns>
        [HttpPut("me")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid token");

            var result = await _repository.UpdateProfileAsync(userId.Value, request);
            return Ok(result);
        }

        /// <summary>
        /// Changes the password of the currently authenticated HR user.
        /// </summary>
        /// <param name="request">The password change payload containing current and new passwords.</param>
        /// <returns>The result of the password change operation.</returns>
        [HttpPost("change-password")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid token");

            var result = await _repository.ChangePasswordAsync(userId.Value, request);
            return Ok(result);
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) return null;
            return int.TryParse(claim.Value, out int id) ? id : null;
        }
    }
}
