// Repositories/Implementations/AttendanceRepository.cs
// Full redraft — checkout removed, geofence moved to SP

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZoomAttendance.Helpers;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Repositories.Implementations
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public AttendanceRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // ── Initialize ────────────────────────────────────────────────────────
        public async Task InitializeAsync(int meetingId, int staffId, string mode)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_InitializeAttendance", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MeetingId", meetingId);
            command.Parameters.AddWithValue("@StaffId", staffId);
            command.Parameters.AddWithValue("@Mode", mode);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        // ── Physical Check-In ─────────────────────────────────────────────────
        public async Task<CheckInResponse> PhysicalCheckInAsync(string token, decimal latitude, decimal longitude)
        {
            var claims = ValidateInviteToken(token);
            var staffId = int.Parse(claims.First(c => c.Type == "staffId").Value);
            var meetingId = int.Parse(claims.First(c => c.Type == "meetingId").Value);

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_PhysicalCheckIn", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);
            command.Parameters.AddWithValue("@StaffId", staffId);
            command.Parameters.AddWithValue("@Latitude", latitude);
            command.Parameters.AddWithValue("@Longitude", longitude);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode))
                    throw new InvalidOperationException($"{errorCode}:{reader["ErrorMessage"]}");

                return new CheckInResponse
                {
                    Status = reader["Status"].ToString()!,
                    Message = "Checked in successfully."
                };
            }

            throw new InvalidOperationException("Unexpected error during check-in.");
        }

        // ── Virtual Join ──────────────────────────────────────────────────────
        public async Task<VirtualJoinResponse> VirtualJoinAsync(string token)
        {
            var claims = ValidateInviteToken(token);
            var staffId = int.Parse(claims.First(c => c.Type == "staffId").Value);
            var meetingId = int.Parse(claims.First(c => c.Type == "meetingId").Value);

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_VirtualJoin", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);
            command.Parameters.AddWithValue("@StaffId", staffId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode))
                    throw new InvalidOperationException(reader["ErrorMessage"].ToString());

                return new VirtualJoinResponse
                {
                    ZoomUrl = reader["ZoomUrl"].ToString()!,
                    Message = "Attendance recorded. Redirecting to Zoom."
                };
            }

            throw new InvalidOperationException("Unexpected error during virtual join.");
        }

        // ── Virtual End Confirm ───────────────────────────────────────────────
        public async Task<CheckInResponse> VirtualEndConfirmAsync(string token)
        {
            var claims = ValidateEndConfirmToken(token);
            var staffId = int.Parse(claims.First(c => c.Type == "staffId").Value);
            var meetingId = int.Parse(claims.First(c => c.Type == "meetingId").Value);

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_VirtualEndConfirm", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);
            command.Parameters.AddWithValue("@StaffId", staffId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                if (!string.IsNullOrEmpty(errorCode))
                    throw new InvalidOperationException(reader["ErrorMessage"].ToString());

                return new CheckInResponse
                {
                    Status = "present",
                    Message = "Attendance confirmed. Thank you."
                };
            }

            throw new InvalidOperationException("Unexpected error during end confirmation.");
        }

        // ── Get Attendance By Meeting ─────────────────────────────────────────
        public async Task<PagedAttendanceResponse> GetAttendanceAsync(int meetingId, AttendanceFilterRequest filter)
        {
            var records = new List<AttendanceResponse>();
            int total = 0;

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAttendanceByMeeting", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);
            command.Parameters.AddWithValue("@Status", (object?)filter.Status ?? DBNull.Value);
            command.Parameters.AddWithValue("@DepartmentId", (object?)filter.DepartmentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Page", filter.Page);
            command.Parameters.AddWithValue("@Limit", filter.Limit);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("TotalCount"));

                records.Add(MapFromReader(reader));
            }

            return new PagedAttendanceResponse
            {
                Data = records,
                Total = total,
                Page = filter.Page,
                Limit = filter.Limit,
                TotalPages = (int)Math.Ceiling((double)total / filter.Limit)
            };
        }

        // ── Get Summary ───────────────────────────────────────────────────────
        public async Task<AttendanceSummaryResponse> GetSummaryAsync(int meetingId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAttendanceSummary", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MeetingId", meetingId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new AttendanceSummaryResponse
                {
                    Total = reader.GetInt32(reader.GetOrdinal("Total")),
                    Present = reader.GetInt32(reader.GetOrdinal("Present")),
                    Absent = reader.GetInt32(reader.GetOrdinal("Absent")),
                    Late = reader.GetInt32(reader.GetOrdinal("Late")),
                    LeftEarly = reader.GetInt32(reader.GetOrdinal("LeftEarly")),
                    Joined = reader.GetInt32(reader.GetOrdinal("Joined")),
                    CheckedIn = reader.GetInt32(reader.GetOrdinal("CheckedIn"))
                };
            }

            return new AttendanceSummaryResponse();
        }

        // ── Manual Status Override ────────────────────────────────────────────
        public async Task UpdateStatusAsync(int meetingId, int staffId, string status)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_UpdateAttendanceStatus", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MeetingId", meetingId);
            command.Parameters.AddWithValue("@StaffId", staffId);
            command.Parameters.AddWithValue("@Status", status);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        // ── Export ────────────────────────────────────────────────────────────
        public async Task<byte[]> ExportAsync(int meetingId, AttendanceFilterRequest filter)
        {
            var records = new List<AttendanceResponse>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_ExportAttendance", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);
            command.Parameters.AddWithValue("@Status", (object?)filter.Status ?? DBNull.Value);
            command.Parameters.AddWithValue("@DepartmentId", (object?)filter.DepartmentId ?? DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                records.Add(new AttendanceResponse
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    MeetingId = reader.GetInt32(reader.GetOrdinal("MeetingId")),
                    StaffId = reader.GetInt32(reader.GetOrdinal("StaffId")),
                    StaffName = reader.GetString(reader.GetOrdinal("StaffName")),
                    StaffEmail = reader.GetString(reader.GetOrdinal("StaffEmail")),
                    DepartmentName = reader.GetString(reader.GetOrdinal("DepartmentName")),
                    Mode = reader.GetString(reader.GetOrdinal("Mode")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CheckInAt = reader.IsDBNull(reader.GetOrdinal("CheckInAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CheckInAt")),
                    CheckInWithinFence = reader.IsDBNull(reader.GetOrdinal("CheckInWithinFence")) ? null : reader.GetBoolean(reader.GetOrdinal("CheckInWithinFence")),
                    JoinedAt = reader.IsDBNull(reader.GetOrdinal("JoinedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("JoinedAt")),
                    ConfirmedAt = reader.IsDBNull(reader.GetOrdinal("ConfirmedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ConfirmedAt")),
                });
            }

            var headers = new[]
            {
                "Staff Name", "Email", "Department", "Mode", "Status",
                "Check-In Time", "Within Fence",
                "Joined At", "Confirmed At"
            };

            var rows = records.Select(a => new List<object?>
            {
                a.StaffName,
                a.StaffEmail,
                a.DepartmentName,
                a.Mode,
                a.Status,
                a.CheckInAt?.ToString("yyyy-MM-dd HH:mm:ss")   ?? "-",
                a.CheckInWithinFence.HasValue ? (a.CheckInWithinFence.Value ? "Yes" : "No") : "-",
                a.JoinedAt?.ToString("yyyy-MM-dd HH:mm:ss")    ?? "-",
                a.ConfirmedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-",
            });

            return ExcelExportHelper.GenerateExcel("Attendance", headers, rows);
        }

        // ── Get Pending Virtual Confirms (for background job) ─────────────────
        public async Task<List<PendingVirtualConfirm>> GetPendingVirtualConfirmsAsync()
        {
            var list = new List<PendingVirtualConfirm>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetPendingVirtualConfirms", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new PendingVirtualConfirm
                {
                    MeetingId = reader.GetInt32(reader.GetOrdinal("MeetingId")),
                    StaffId = reader.GetInt32(reader.GetOrdinal("StaffId")),
                    StaffName = reader.GetString(reader.GetOrdinal("StaffName")),
                    StaffEmail = reader.GetString(reader.GetOrdinal("StaffEmail")),
                    MeetingTitle = reader.GetString(reader.GetOrdinal("MeetingTitle")),
                    EndConfirmToken = reader.GetString(reader.GetOrdinal("EndConfirmToken")),
                });
            }

            return list;
        }

        // ── Save End Confirm Token ────────────────────────────────────────────
        public async Task SaveEndConfirmTokenAsync(int meetingId, int staffId, string token)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_SaveEndConfirmToken", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MeetingId", meetingId);
            command.Parameters.AddWithValue("@StaffId", staffId);
            command.Parameters.AddWithValue("@Token", token);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private IEnumerable<Claim> ValidateInviteToken(string token)
        {
            var secret = _configuration["AppSettings:JwtInviteSecret"]!;
            var handler = new JwtSecurityTokenHandler();
            var result = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            return result.Claims;
        }

        private IEnumerable<Claim> ValidateEndConfirmToken(string token)
        {
            var claims = ValidateInviteToken(token);
            if (claims.FirstOrDefault(c => c.Type == "type")?.Value != "endconfirm")
                throw new SecurityTokenException("Invalid token type.");
            return claims;
        }

        private static AttendanceResponse MapFromReader(SqlDataReader reader)
        {
            return new AttendanceResponse
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                MeetingId = reader.GetInt32(reader.GetOrdinal("MeetingId")),
                StaffId = reader.GetInt32(reader.GetOrdinal("StaffId")),
                StaffName = reader.GetString(reader.GetOrdinal("StaffName")),
                StaffEmail = reader.GetString(reader.GetOrdinal("StaffEmail")),
                DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
                DepartmentName = reader.GetString(reader.GetOrdinal("DepartmentName")),
                Mode = reader.GetString(reader.GetOrdinal("Mode")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CheckInAt = reader.IsDBNull(reader.GetOrdinal("CheckInAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CheckInAt")),
                CheckInWithinFence = reader.IsDBNull(reader.GetOrdinal("CheckInWithinFence")) ? null : reader.GetBoolean(reader.GetOrdinal("CheckInWithinFence")),
                JoinedAt = reader.IsDBNull(reader.GetOrdinal("JoinedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("JoinedAt")),
                ConfirmedAt = reader.IsDBNull(reader.GetOrdinal("ConfirmedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ConfirmedAt")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            };
        }
    }
}