using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZoomAttendance.Data;
using ZoomAttendance.Models.Entities;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Models.ResponseModels.ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Repositories.Implementations
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public AuthRepository(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResponse<LoginResponse>.Fail("Invalid login request");

                if (string.IsNullOrWhiteSpace(request.Email))
                    return ApiResponse<LoginResponse>.Fail("Email is required");

                if (!IsValidEmail(request.Email))
                    return ApiResponse<LoginResponse>.Fail("Invalid email format");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return ApiResponse<LoginResponse>.Fail("Password is required");

                var normalizedEmail = request.Email.Trim().ToLower();

                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

                if (user == null)
                    return ApiResponse<LoginResponse>.Fail("Invalid email or password");

                bool isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

                if (!isValidPassword)
                    return ApiResponse<LoginResponse>.Fail("Invalid email or password");

                var token = GenerateJwtToken(user);

                return ApiResponse<LoginResponse>.Success(new LoginResponse
                {
                    UserId = user.UserId,
                    FullName = user.StaffName,
                    Email = user.Email,
                    Role = user.Role,
                    Token = token
                }, "Login successful");
            }
            catch (Exception ex)
            {
                return ApiResponse<LoginResponse>.Fail($"Login failed: {ex.Message} | {ex.InnerException?.Message}");
            }
        }

        public Task<ApiResponse<string>> LogoutAsync()
        {
            return Task.FromResult(ApiResponse<string>.Success("Logout successful"));
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }
    }
}