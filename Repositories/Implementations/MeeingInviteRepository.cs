// Repositories/Implementations/MeetingInviteRepository.cs
// StaffId changed to Guid

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;
using ZoomAttendance.Services;

namespace ZoomAttendance.Repositories.Implementations
{
    public class MeetingInviteRepository : IMeetingInviteRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IAttendanceRepository _attendanceRepo;

        public MeetingInviteRepository(
            IConfiguration configuration,
            IEmailService emailService,
            IAttendanceRepository attendanceRepo)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _emailService = emailService;
            _attendanceRepo = attendanceRepo;
        }

        // ── Emails Preview ────────────────────────────────────────────────────
        public async Task<List<MeetingEmailPreviewResponse>> GetEmailsPreviewAsync(int meetingId)
        {
            var results = new List<MeetingEmailPreviewResponse>();

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
                // Check if this is an error row
                try
                {
                    var errorCode = reader["ErrorCode"]?.ToString();
                    if (!string.IsNullOrEmpty(errorCode))
                        throw new KeyNotFoundException(reader["ErrorMessage"].ToString());
                }
                catch (IndexOutOfRangeException)
                {
                    // No ErrorCode column — this is a real data row
                }

                results.Add(new MeetingEmailPreviewResponse
                {
                    StaffId = reader.GetGuid(reader.GetOrdinal("StaffId")),
                    StaffName = reader.GetString(reader.GetOrdinal("StaffName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    DepartmentName = reader.GetString(reader.GetOrdinal("DepartmentName")),
                });
            }

            return results;
        }

        // ── Send Invites ──────────────────────────────────────────────────────
        public async Task<SendInvitesResponse> SendInvitesAsync(int meetingId)
        {
            var previews = await GetEmailsPreviewAsync(meetingId);

            // Get meeting details
            var meeting = await GetMeetingDetailsAsync(meetingId);
            
            int sent = 0, failed = 0;

            foreach (var staff in previews)
            {
                try
                {
                    var expiresAt = meeting.StartDatetime.AddMinutes(meeting.DurationMinutes);
                    var token = GenerateInviteToken(staff.StaffId, meetingId, expiresAt);
                   
                    await SaveInviteAsync(meetingId, staff.StaffId, token, expiresAt);
                    await _attendanceRepo.InitializeAsync(meetingId, staff.StaffId, (string)meeting.Mode);
                    await _emailService.SendAttendanceLinkEmailAsync(
     new ZoomAttendance.Entities.Staff { Name = staff.StaffName, Email = staff.Email },
     new ZoomAttendance.Entities.Meeting
     {
         Id = meeting.Id,
         Title = meeting.Title,
         Mode = meeting.Mode,
         StartDatetime = meeting.StartDatetime,
         DurationMinutes = meeting.DurationMinutes,
         ZoomUrl = meeting.ZoomUrl,
         Location = meeting.Location,
     },
     token);

                    sent++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send invite to {staff.Email}: {ex.Message} | {ex.InnerException?.Message}");
                    failed++;
                }
            }

            return new SendInvitesResponse
            {
                TotalStaff = previews.Count,
                Sent = sent,
                Failed = failed,
                Message = $"Invites sent: {sent}, Failed: {failed}."
            };
        }

        // ── Resend Invite ─────────────────────────────────────────────────────
        public async Task ResendInviteAsync(int meetingId, Guid staffId)
        {
            var meeting = await GetMeetingDetailsAsync(meetingId);

            // Get staff details
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetStaffById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", staffId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                throw new KeyNotFoundException("Staff not found.");

            var staffName = reader.GetString(reader.GetOrdinal("Name"));
            var staffEmail = reader.GetString(reader.GetOrdinal("Email"));
            await reader.CloseAsync();

            var expiresAt = meeting.StartDatetime.AddMinutes(meeting.DurationMinutes);
            var token = GenerateInviteToken(staffId, meetingId, expiresAt);

            await SaveInviteAsync(meetingId, staffId, token, expiresAt);
            await _emailService.SendAttendanceLinkEmailAsync(
     new ZoomAttendance.Entities.Staff { Name = staffName, Email = staffEmail },
     new ZoomAttendance.Entities.Meeting
     {
         Id = meeting.Id,
         Title = meeting.Title,
         Mode = meeting.Mode,
         StartDatetime = meeting.StartDatetime,
         DurationMinutes = meeting.DurationMinutes,
         ZoomUrl = meeting.ZoomUrl,
         Location = meeting.Location,
     },
     token);
        }

        // ── Get Invites By Meeting ────────────────────────────────────────────
        public async Task<List<MeetingInviteResponse>> GetInvitesByMeetingAsync(int meetingId)
        {
            var results = new List<MeetingInviteResponse>();

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
                results.Add(new MeetingInviteResponse
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    MeetingId = reader.GetInt32(reader.GetOrdinal("MeetingId")),
                    StaffId = reader.GetGuid(reader.GetOrdinal("StaffId")),
                    StaffName = reader.GetString(reader.GetOrdinal("StaffName")),
                    StaffEmail = reader.GetString(reader.GetOrdinal("StaffEmail")),
                    Token = reader.GetString(reader.GetOrdinal("Token")),
                    SentAt = reader.IsDBNull(reader.GetOrdinal("SentAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SentAt")),
                    ResentAt = reader.IsDBNull(reader.GetOrdinal("ResentAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ResentAt")),
                    ExpiresAt = reader.IsDBNull(reader.GetOrdinal("ExpiresAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                    ConfirmedAt = reader.IsDBNull(reader.GetOrdinal("ConfirmedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ConfirmedAt")),
                    JoinedAt = reader.IsDBNull(reader.GetOrdinal("JoinedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("JoinedAt")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                });
            }

            return results;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private async Task SaveInviteAsync(int meetingId, Guid staffId, string token, DateTime expiresAt)
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

        private string GenerateInviteToken(Guid staffId, int meetingId, DateTime expiresAt)
        {
            var secret = _configuration["AppSettings:JwtInviteSecret"]!;
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("staffId",   staffId.ToString()),
                    new Claim("meetingId", meetingId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                }),
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        private async Task<dynamic> GetMeetingDetailsAsync(int meetingId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetMeetingById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", meetingId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                throw new KeyNotFoundException($"Meeting {meetingId} not found.");

            return new
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Mode = reader.GetString(reader.GetOrdinal("Mode")),
                StartDatetime = reader.GetDateTime(reader.GetOrdinal("StartDatetime")),
                DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
                ZoomUrl = reader.IsDBNull(reader.GetOrdinal("ZoomUrl")) ? null : reader.GetString(reader.GetOrdinal("ZoomUrl")),
                Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
            };
        }
    }
}