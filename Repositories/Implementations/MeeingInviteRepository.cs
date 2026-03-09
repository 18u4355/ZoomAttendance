// Repositories/Implementations/MeetingInviteRepository.cs

using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZoomAttendance.Entities;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;
using ZoomAttendance.Services;

namespace ZoomAttendance.Repositories.Implementations
{
    public class MeetingInviteRepository : IMeetingInviteRepository
    {
        private readonly string _connectionString;
        private readonly IEmailService _emailService;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly string _baseUrl;
        private readonly string _jwtSecret;

        public MeetingInviteRepository(
            IConfiguration configuration,
            IEmailService emailService,
            IAttendanceRepository attendanceRepo)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _emailService = emailService;
            _attendanceRepo = attendanceRepo;
            _baseUrl = configuration["AppSettings:BaseUrl"]!;
            _jwtSecret = configuration["AppSettings:JwtInviteSecret"]!;
        }

        // ── Get Emails Preview ────────────────────────────────────────────────
        public async Task<List<MeetingEmailPreviewResponse>> GetEmailsPreviewAsync(int meetingId)
        {
            var result = new List<MeetingEmailPreviewResponse>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetMeetingEmailsPreview", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new MeetingEmailPreviewResponse
                {
                    StaffId = reader.GetInt32(reader.GetOrdinal("StaffId")),
                    StaffName = reader.GetString(reader.GetOrdinal("StaffName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Department = reader.GetString(reader.GetOrdinal("DepartmentName"))
                });
            }

            return result;
        }

        // ── Send Invites ──────────────────────────────────────────────────────
        public async Task<SendInvitesResponse> SendInvitesAsync(int meetingId)
        {
            var meeting = await GetMeetingAsync(meetingId)
                ?? throw new KeyNotFoundException($"Meeting with id '{meetingId}' was not found.");

            if (meeting.Status == "cancelled")
                throw new InvalidOperationException("Cannot send invites for a cancelled meeting.");

            var staffPreviews = await GetEmailsPreviewAsync(meetingId);

            int sent = 0;
            int failed = 0;

            foreach (var staffPreview in staffPreviews)
            {
                try
                {
                    var expiresAt = meeting.StartDatetime.AddMinutes(meeting.DurationMinutes);
                    var token = GenerateInviteToken(staffPreview.StaffId, meetingId, expiresAt);

                    await SaveInviteAsync(meetingId, staffPreview.StaffId, token, expiresAt, isResend: false);

                    var staff = new Staff
                    {
                        Id = staffPreview.StaffId,
                        Name = staffPreview.StaffName,
                        Email = staffPreview.Email
                    };

                    await _emailService.SendAttendanceLinkEmailAsync(staff, meeting, token);
                    await _attendanceRepo.InitializeAsync(meetingId, staffPreview.StaffId, meeting.Mode);

                    sent++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send invite to {staffPreview.Email}: {ex.Message}");
                    failed++;
                }
            }

            return new SendInvitesResponse
            {
                TotalStaff = staffPreviews.Count,
                Sent = sent,
                Failed = failed,
                Message = $"Invites sent: {sent}, Failed: {failed}."
            };
        }

        // ── Resend Invite ─────────────────────────────────────────────────────
        public async Task ResendInviteAsync(int meetingId, int staffId)
        {
            var meeting = await GetMeetingAsync(meetingId)
                ?? throw new KeyNotFoundException($"Meeting with id '{meetingId}' was not found.");

            if (meeting.Status == "cancelled")
                throw new InvalidOperationException("Cannot resend invite for a cancelled meeting.");

            var staff = await GetStaffAsync(staffId)
                ?? throw new KeyNotFoundException($"Staff with id '{staffId}' was not found.");

            var expiresAt = meeting.StartDatetime.AddMinutes(meeting.DurationMinutes);
            var token = GenerateInviteToken(staffId, meetingId, expiresAt);

            await SaveInviteAsync(meetingId, staffId, token, expiresAt, isResend: true);
            await _emailService.SendAttendanceLinkEmailAsync(staff, meeting, token);
        }

        // ── Get Invites By Meeting ────────────────────────────────────────────
        public async Task<List<MeetingInviteResponse>> GetInvitesByMeetingAsync(int meetingId)
        {
            var result = new List<MeetingInviteResponse>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetMeetingInvites", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new MeetingInviteResponse
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    MeetingId = reader.GetInt32(reader.GetOrdinal("MeetingId")),
                    StaffId = reader.GetInt32(reader.GetOrdinal("StaffId")),
                    StaffName = reader.GetString(reader.GetOrdinal("StaffName")),
                    StaffEmail = reader.GetString(reader.GetOrdinal("StaffEmail")),
                    Department = reader.GetString(reader.GetOrdinal("Department")),
                    SentAt = reader.IsDBNull(reader.GetOrdinal("SentAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SentAt")),
                    ResentAt = reader.IsDBNull(reader.GetOrdinal("ResentAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ResentAt")),
                    ExpiresAt = reader.IsDBNull(reader.GetOrdinal("ExpiresAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                    ConfirmedAt = reader.IsDBNull(reader.GetOrdinal("ConfirmedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ConfirmedAt")),
                    JoinedAt = reader.IsDBNull(reader.GetOrdinal("JoinedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("JoinedAt")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }

            return result;
        }

        // ── Save Meeting Location (Geofence) ──────────────────────────────────
        public async Task SaveLocationAsync(SaveMeetingLocationRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_SaveMeetingLocation", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", request.MeetingId);
            command.Parameters.AddWithValue("@Latitude", request.Latitude);
            command.Parameters.AddWithValue("@Longitude", request.Longitude);
            command.Parameters.AddWithValue("@RadiusMetres", request.RadiusMetres);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private string GenerateInviteToken(int staffId, int meetingId, DateTime expiresAt)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("staffId",   staffId.ToString()),
                new Claim("meetingId", meetingId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task SaveInviteAsync(
            int meetingId, int staffId, string token, DateTime expiresAt, bool isResend)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_SaveMeetingInvite", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);
            command.Parameters.AddWithValue("@StaffId", staffId);
            command.Parameters.AddWithValue("@Token", token);
            command.Parameters.AddWithValue("@ExpiresAt", expiresAt);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        private async Task<Meeting?> GetMeetingAsync(int meetingId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetMeetingById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Id", meetingId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Meeting
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Mode = reader.GetString(reader.GetOrdinal("Mode")),
                    AudienceType = reader.GetString(reader.GetOrdinal("AudienceType")),
                    StartDatetime = reader.GetDateTime(reader.GetOrdinal("StartDatetime")),
                    DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
                    Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
                    ZoomUrl = reader.IsDBNull(reader.GetOrdinal("ZoomUrl")) ? null : reader.GetString(reader.GetOrdinal("ZoomUrl")),
                    Status = reader.GetString(reader.GetOrdinal("Status"))
                };
            }

            return null;
        }

        private async Task<Staff?> GetStaffAsync(int staffId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetStaffById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Id", staffId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Staff
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Email = reader.GetString(reader.GetOrdinal("Email"))
                };
            }

            return null;
        }
    }
}