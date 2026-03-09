// Repositories/Implementations/DashboardRepository.cs

using Microsoft.Data.SqlClient;
using System.Data;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        public DashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<DashboardResponse> GetStatsAsync()
        {
            var response = new DashboardResponse();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetDashboardStats", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // ── Result Set 1: Counts ──────────────────────────────────────────
            if (await reader.ReadAsync())
            {
                response.Counts = new DashboardCountsResponse
                {
                    TotalMeetings = reader.GetInt32(reader.GetOrdinal("TotalMeetings")),
                    UpcomingMeetings = reader.GetInt32(reader.GetOrdinal("UpcomingMeetings")),
                    TotalActiveStaff = reader.GetInt32(reader.GetOrdinal("TotalActiveStaff")),
                    TotalDepartments = reader.GetInt32(reader.GetOrdinal("TotalDepartments"))
                };
            }

            // ── Result Set 2: Attendance Summary ──────────────────────────────
            await reader.NextResultAsync();
            if (await reader.ReadAsync())
            {
                response.AttendanceSummary = new DashboardAttendanceResponse
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

            // ── Result Set 3: Upcoming Meetings ───────────────────────────────
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                response.UpcomingMeetings.Add(new UpcomingMeetingResponse
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Mode = reader.GetString(reader.GetOrdinal("Mode")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    StartDatetime = reader.GetDateTime(reader.GetOrdinal("StartDatetime")),
                    DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
                    Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
                    ZoomUrl = reader.IsDBNull(reader.GetOrdinal("ZoomUrl")) ? null : reader.GetString(reader.GetOrdinal("ZoomUrl"))
                });
            }

            // ── Quick Actions (static) ────────────────────────────────────────
            response.QuickActions = new List<QuickActionResponse>
            {
                new() { Label = "Create Meeting",    Action = "create",   Route = "/api/v1/meetings" },
                new() { Label = "View Staff",        Action = "navigate", Route = "/api/v1/staff" },
                new() { Label = "View Departments",  Action = "navigate", Route = "/api/v1/departments" }
            };

            return response;
        }
    }
}