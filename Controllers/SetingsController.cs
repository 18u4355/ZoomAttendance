using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZoomAttendance.Models.RequestModels;

namespace ZoomAttendance.Controllers
{
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

        [HttpGet("me")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetMySettings()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid token");

            var result = await _repository.GetMySettingsAsync(userId.Value);
            return Ok(result);
        }

        [HttpPut("me")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid token");

            var result = await _repository.UpdateProfileAsync(userId.Value, request);
            return Ok(result);
        }

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