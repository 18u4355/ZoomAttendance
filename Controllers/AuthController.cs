using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    /// <summary>
    /// Handles authentication endpoints for signing HR users into and out of the API.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;

        public AuthController(IAuthRepository authRepo)
        {
            _authRepo = authRepo;
        }


        /// <summary>
        /// Authenticates a user with email and password and returns the login payload used by the client.
        /// </summary>
        /// <param name="request">Login credentials submitted by the client.</param>
        /// <returns>A successful login response when the credentials are valid, or an error response when authentication fails.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authRepo.LoginAsync(request);

            if (!result.IsSuccessful)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Logs out the currently authenticated user from the application session context.
        /// </summary>
        /// <returns>A standard API response confirming logout completion.</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var result = await _authRepo.LogoutAsync();
            return Ok(result);
        }

    }
}
