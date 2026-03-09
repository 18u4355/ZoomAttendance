// BackgroundJobs/AttendanceBackgroundJob.cs
// Checkout job removed — only handles virtual end confirm emails now

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ZoomAttendance.Repositories.Interfaces;
using ZoomAttendance.Services;

namespace ZoomAttendance.BackgroundJobs
{
    public class AttendanceBackgroundJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AttendanceBackgroundJob> _logger;

        public AttendanceBackgroundJob(IServiceProvider serviceProvider, ILogger<AttendanceBackgroundJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessEndConfirmEmailsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AttendanceBackgroundJob");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        // Send end confirmation emails to virtual/hybrid-virtual staff when meeting ends
        private async Task ProcessEndConfirmEmailsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var attendanceRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
            var meetingRepo = scope.ServiceProvider.GetRequiredService<IMeetingRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var pendingConfirms = await attendanceRepo.GetPendingVirtualConfirmsAsync();

            foreach (var item in pendingConfirms)
            {
                try
                {
                    // Generate end confirm token
                    var secret = configuration["AppSettings:JwtInviteSecret"]!;
                    var baseUrl = configuration["AppSettings:BaseUrl"]!;
                    var expiresAt = DateTime.UtcNow.AddMinutes(30);

                    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                                           System.Text.Encoding.UTF8.GetBytes(secret));

                    var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
                    {
                        Subject = new System.Security.Claims.ClaimsIdentity(new[]
                        {
                            new System.Security.Claims.Claim("staffId",   item.StaffId.ToString()),
                            new System.Security.Claims.Claim("meetingId", item.MeetingId.ToString()),
                            new System.Security.Claims.Claim("type",      "endconfirm"),
                        }),
                        Expires = expiresAt,
                        SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                                                key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)
                    };

                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var tokenString = tokenHandler.WriteToken(token);

                    // Save token
                    await attendanceRepo.SaveEndConfirmTokenAsync(item.MeetingId, item.StaffId, tokenString);

                    // Build confirm link
                    var confirmLink = $"{baseUrl}/attendance/confirm-end?token={tokenString}";

                    // Send email
                    var subject = $"Please confirm your attendance — {item.MeetingTitle}";
                    var body = $@"
                        <p>Hi {item.StaffName},</p>
                        <p>The meeting <strong>{item.MeetingTitle}</strong> has ended.</p>
                        <p>Please click the button below to confirm your attendance. This link expires in 30 minutes.</p>
                        <p style='margin:24px 0;'>
                            <a href='{confirmLink}'
                               style='background:#1A5FAB;color:#fff;padding:12px 24px;
                                      border-radius:6px;text-decoration:none;font-weight:bold;'>
                                Confirm Attendance
                            </a>
                        </p>
                        <p style='color:#999;font-size:12px;'>This link is unique to you. Do not share it.</p>
                        <p>Regards,<br/>HR Team</p>";

                    await emailService.SendEmailAsync(item.StaffEmail, subject, body);

                    _logger.LogInformation(
                        "End confirm email sent to {Email} for meeting {MeetingId}",
                        item.StaffEmail, item.MeetingId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send end confirm email to StaffId {StaffId} for MeetingId {MeetingId}",
                        item.StaffId, item.MeetingId);
                }
            }
        }
    }
}