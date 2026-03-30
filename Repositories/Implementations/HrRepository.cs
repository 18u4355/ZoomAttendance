using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZoomAttendance.Data;
using ZoomAttendance.Models.Entities;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;
using ZoomAttendance.Services;

namespace ZoomAttendance.Repositories.Implementations
{
    public class HrRepository : IHrRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public HrRepository(ApplicationDbContext db, IConfiguration config, IEmailService emailService)
        {
            _db = db;
            _config = config;
            _emailService = emailService;
        }

        // ── Step 1: Admin invites a new HR user ───────────────────────────────
        public async Task<ApiResponse<string>> InviteHrAsync(InviteHrRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.StaffName))
                    return ApiResponse<string>.Fail("Staff name is required");

                if (string.IsNullOrWhiteSpace(request.Email))
                    return ApiResponse<string>.Fail("Email is required");

                var email = request.Email.Trim().ToLower();

                if (!IsValidEmail(email))
                    return ApiResponse<string>.Fail("Invalid email format");

                // Check if a user with this email already exists and is active
                var existingUser = await _db.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

                if (existingUser != null && existingUser.IsActive)
                    return ApiResponse<string>.Fail("A user with this email already exists");

                // If a pending (inactive) invite exists, remove it so we can re-invite
                if (existingUser != null && !existingUser.IsActive)
                    _db.Users.Remove(existingUser);

                // Create a placeholder user row — password is null until they complete setup
                var newUser = new User
                {
                    StaffName = request.StaffName.Trim(),
                    Email = email,
                    Role = "HR",
                    Department = request.Department?.Trim(),
                    PasswordHash = null,   // set during CompleteHrSetup
                    IsActive = false,      // activated once they set their password
                    CreatedAt = DateTime.UtcNow
                };

                _db.Users.Add(newUser);
                await _db.SaveChangesAsync();

                // Generate a short-lived setup token (24 h)
                var token = GenerateSetupToken(newUser.UserId, email);

                var baseUrl = _config["AppSettings:BaseUrl"]!.TrimEnd('/');
                var setupLink = $"{baseUrl}/set-password?token={token}";

                var subject = "You've been invited to MeetTrack – Set your password";
                var body = BuildInviteEmailBody(request.StaffName.Trim(), setupLink);

                await _emailService.SendEmailAsync(email, subject, body);

                return ApiResponse<string>.Success("Invitation sent successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to send invitation", ex.Message);
            }
        }

        // ── Step 2: HR clicks the link and sets their password ────────────────
        public async Task<ApiResponse<string>> CompleteHrSetupAsync(CompleteHrSetupRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Token))
                    return ApiResponse<string>.Fail("Token is required");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return ApiResponse<string>.Fail("Password is required");

                if (request.Password.Length < 6)
                    return ApiResponse<string>.Fail("Password must be at least 6 characters");

                if (request.Password != request.ConfirmPassword)
                    return ApiResponse<string>.Fail("Passwords do not match");

                // Validate and decode the setup token
                var (userId, email) = ValidateSetupToken(request.Token);

                if (userId == null || email == null)
                    return ApiResponse<string>.Fail("Invalid or expired invitation link. Please contact your administrator.");

                var user = await _db.Users.FirstOrDefaultAsync(u =>
                    u.UserId == userId && u.Email.ToLower() == email.ToLower());

                if (user == null)
                    return ApiResponse<string>.Fail("User not found");

                if (user.IsActive)
                    return ApiResponse<string>.Fail("This account has already been set up. Please login.");

                // Populate password and activate the account
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return ApiResponse<string>.Success("Account setup complete. You can now log in.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to complete account setup", ex.Message);
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private string GenerateSetupToken(int userId, string email)
        {
            var secret = _config["AppSettings:JwtInviteSecret"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("purpose", "hr-setup")
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private (int? userId, string? email) ValidateSetupToken(string token)
        {
            try
            {
                var secret = _config["AppSettings:JwtInviteSecret"]!;
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                var purpose = principal.FindFirst("purpose")?.Value;
                if (purpose != "hr-setup") return (null, null);

                var userIdStr = principal.FindFirst("userId")?.Value;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;

                if (!int.TryParse(userIdStr, out int userId)) return (null, null);

                return (userId, email);
            }
            catch
            {
                return (null, null);
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch { return false; }
        }

        private static string BuildInviteEmailBody(string staffName, string setupLink)
        {
            return $@"
                <html>
                <body style='font-family:Arial,sans-serif;color:#333;background:#f6f8fb;padding:30px;'>
                    <div style='max-width:600px;margin:auto;background:white;padding:36px;border-radius:10px;box-shadow:0 2px 8px rgba(0,0,0,0.08);'>
                        <h2 style='color:#2d8cff;margin-top:0;'>Welcome to MeetTrack!</h2>
                        <p>Hello <strong>{staffName}</strong>,</p>
                        <p>You've been added as an <strong>HR user</strong> on the MeetTrack platform. 
                           To get started, please set your password by clicking the button below.</p>
                        <p style='margin:28px 0;'>
                            <a href='{setupLink}'
                               style='background:#2d8cff;color:white;padding:14px 28px;
                                      text-decoration:none;border-radius:6px;
                                      font-weight:bold;display:inline-block;'>
                                Set My Password
                            </a>
                        </p>
                        <p style='font-size:13px;color:#888;'>
                            This link will expire in <strong>24 hours</strong>. 
                            If you didn't expect this invitation, you can safely ignore this email.
                        </p>
                        <hr style='border:none;border-top:1px solid #eee;margin:24px 0;'/>
                        <p style='font-size:12px;color:#aaa;margin:0;'>MeetTrack — Attendance Management System</p>
                    </div>
                </body>
                </html>";
        }
    }
}