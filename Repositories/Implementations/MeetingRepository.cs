using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using ZoomAttendance.Helpers;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;
using ZoomAttendance.Services;

namespace ZoomAttendance.Repositories.Implementations
{
    public class MeetingRepository : IMeetingRepository
    {
        private readonly string _connectionString;
        private readonly IZoomService _zoomService;

        public MeetingRepository(IConfiguration configuration, IZoomService zoomService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _zoomService = zoomService;
        }

        public async Task<PagedMeetingResponse> GetAllAsync(MeetingFilterRequest filter)
        {
            var meetings = new List<MeetingResponse>();
            int totalCount = 0;

            var limit = filter.Limit is < 1 or > 100 ? 20 : filter.Limit;
            var page = filter.Page < 1 ? 1 : filter.Page;

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAllMeetings", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Search", (object?)filter.Search ?? DBNull.Value);
            command.Parameters.AddWithValue("@Mode", (object?)filter.Mode ?? DBNull.Value);
            command.Parameters.AddWithValue("@AudienceType", (object?)filter.AudienceType ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (object?)filter.Status ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateFrom", (object?)filter.DateFrom ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateTo", (object?)filter.DateTo ?? DBNull.Value);
            command.Parameters.AddWithValue("@DepartmentId", (object?)filter.DepartmentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Page", page);
            command.Parameters.AddWithValue("@Limit", limit);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (totalCount == 0)
                    totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));

                meetings.Add(MapToResponse(reader));
            }

            return new PagedMeetingResponse
            {
                Data = meetings,
                Page = page,
                Limit = limit,
                Total = totalCount
            };
        }

        public async Task<MeetingResponse?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetMeetingById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            MeetingResponse? meeting = null;

            // First result set — meeting details
            if (await reader.ReadAsync())
                meeting = MapToResponse(reader);

            if (meeting == null) return null;

            // Second result set — departments
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    meeting.Departments.Add(new MeetingDepartmentResponse
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name"))
                    });
                }
            }

            return meeting;
        }



        public async Task<MeetingResponse> CreateAsync(CreateMeetingRequest request)
        {
            var mode = request.Mode.ToLower().Trim();
            var audienceType = request.AudienceType.ToLower().Trim();        

            string? zoomMeetingId = null;
            string? zoomJoinUrl = null;
            string? zoomStartUrl = null;

            if (mode == "virtual" || mode == "hybrid")
            {
                var zoomMeeting = await _zoomService.CreateMeetingAsync(
                    request.Title.Trim(),
                    request.StartDatetime,
                    request.DurationMinutes);

                zoomMeetingId = zoomMeeting.MeetingId;
                zoomJoinUrl = zoomMeeting.JoinUrl;
                zoomStartUrl = zoomMeeting.StartUrl;
            }

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CreateMeeting", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var departmentIds = request.DepartmentIds != null && request.DepartmentIds.Any()
                ? string.Join(",", request.DepartmentIds) : null;

            var virtualStaffIds = request.VirtualStaffIds != null && request.VirtualStaffIds.Any()
                ? string.Join(",", request.VirtualStaffIds) : null;

            command.Parameters.AddWithValue("@Title", request.Title.Trim());
            command.Parameters.AddWithValue("@Mode", mode);
            command.Parameters.AddWithValue("@AudienceType", audienceType);
            command.Parameters.AddWithValue("@StartDatetime", request.StartDatetime.ToUniversalTime());
            command.Parameters.AddWithValue("@DurationMinutes", request.DurationMinutes);
            command.Parameters.AddWithValue("@Location", (object?)request.Location ?? DBNull.Value);
            command.Parameters.AddWithValue("@VenueId", (object?)request.VenueId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ZoomJoinUrl", (object?)zoomJoinUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@ZoomMeetingId", (object?)zoomMeetingId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ZoomStartUrl", (object?)zoomStartUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@DepartmentIds", (object?)departmentIds ?? DBNull.Value);
            command.Parameters.AddWithValue("@VirtualStaffIds", (object?)virtualStaffIds ?? DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                if (reader.GetName(0) == "ErrorCode")
                    throw new InvalidOperationException(reader["ErrorMessage"].ToString());

                var meeting = MapToResponse(reader);

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        meeting.Departments.Add(new MeetingDepartmentResponse
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name"))
                        });
                    }
                }

                return meeting;
            }

            throw new InvalidOperationException("Failed to create meeting.");
        }

        public async Task<MeetingResponse> UpdateAsync(int id, UpdateMeetingRequest request)
        {
            var mode = request.Mode.ToLower().Trim();
            var audienceType = request.AudienceType.ToLower().Trim();

            var endDatetime = request.StartDatetime.AddMinutes(request.DurationMinutes);
            var inviteScheduledFor = request.StartDatetime.AddMinutes(-10);

            var existing = await GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Meeting not found.");

            string? zoomMeetingId = existing.ZoomMeetingId;
            string? zoomJoinUrl = existing.ZoomJoinUrl;
            string? zoomStartUrl = existing.ZoomStartUrl;

            if (mode == "virtual" || mode == "hybrid")
            {
                if (string.IsNullOrWhiteSpace(zoomMeetingId))
                {
                    var zoomMeeting = await _zoomService.CreateMeetingAsync(
                        request.Title.Trim(),
                        request.StartDatetime,
                        request.DurationMinutes);

                    zoomMeetingId = zoomMeeting.MeetingId;
                    zoomJoinUrl = zoomMeeting.JoinUrl;
                    zoomStartUrl = zoomMeeting.StartUrl;
                }
                else
                {
                    await _zoomService.UpdateMeetingAsync(
                        zoomMeetingId,
                        request.Title.Trim(),
                        request.StartDatetime,
                        request.DurationMinutes);
                }
            }

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_UpdateMeeting", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var departmentIds = request.DepartmentIds != null && request.DepartmentIds.Any()
                ? string.Join(",", request.DepartmentIds)
                : null;

            var virtualStaffIds = request.VirtualStaffIds != null && request.VirtualStaffIds.Any()
                ? string.Join(",", request.VirtualStaffIds)
                : null;
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Title", request.Title.Trim());
            command.Parameters.AddWithValue("@Mode", mode);
            command.Parameters.AddWithValue("@AudienceType", audienceType);
            command.Parameters.AddWithValue("@StartDatetime", request.StartDatetime.ToUniversalTime());
            command.Parameters.AddWithValue("@DurationMinutes", request.DurationMinutes);    
            command.Parameters.AddWithValue("@Location", (object?)request.Location ?? DBNull.Value);
            command.Parameters.AddWithValue("@VenueId", (object?)request.VenueId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ZoomJoinUrl", (object?)zoomJoinUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@ZoomMeetingId", (object?)zoomMeetingId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ZoomStartUrl", (object?)zoomStartUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@DepartmentIds", (object?)departmentIds ?? DBNull.Value);
            command.Parameters.AddWithValue("@VirtualStaffIds", (object?)virtualStaffIds ?? DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                if (reader.GetName(0) == "ErrorCode")
                {
                    var errorCode = reader["ErrorCode"].ToString();
                    var errorMessage = reader["ErrorMessage"].ToString();

                    if (errorCode == "NOT_FOUND")
                        throw new KeyNotFoundException(errorMessage);

                    throw new InvalidOperationException(errorMessage);
                }

                var meeting = MapToResponse(reader);

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        meeting.Departments.Add(new MeetingDepartmentResponse
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name"))
                        });
                    }
                }

                return meeting;
            }

            throw new InvalidOperationException("Failed to update meeting.");
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_DeleteMeeting", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var errorCode = reader["ErrorCode"]?.ToString();
                var errorMessage = reader["ErrorMessage"]?.ToString();

                if (!string.IsNullOrEmpty(errorCode))
                {
                    if (errorCode == "NOT_FOUND")
                        throw new KeyNotFoundException(errorMessage);

                    throw new InvalidOperationException(errorMessage);
                }
            }
        }
 
        public async Task<byte[]> ExportAsync(MeetingFilterRequest filter)
        {
            var records = new List<MeetingResponse>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_ExportMeetings", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Search", (object?)filter.Search ?? DBNull.Value);
            command.Parameters.AddWithValue("@Mode", (object?)filter.Mode ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (object?)filter.Status ?? DBNull.Value);
            command.Parameters.AddWithValue("@AudienceType", (object?)filter.AudienceType ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateFrom", (object?)filter.DateFrom ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateTo", (object?)filter.DateTo ?? DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                records.Add(new MeetingResponse
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Mode = reader.GetString(reader.GetOrdinal("Mode")),
                    AudienceType = reader.GetString(reader.GetOrdinal("AudienceType")),
                    StartDatetime = reader.GetDateTime(reader.GetOrdinal("StartDatetime")),
                    DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
                    Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
                    ZoomJoinUrl = reader.IsDBNull(reader.GetOrdinal("ZoomJoinUrl")) ? null : reader.GetString(reader.GetOrdinal("ZoomJoinUrl")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                });
            }

            var headers = new[]
            {
                "Id", "Title", "Mode", "Audience", "Start Date", "Duration (mins)",
                "Location", "Zoom URL", "Status", "Created At", "Updated At"
            };

            var rows = records.Select(m => new List<object?>
            {
                m.Id,
                m.Title,
                m.Mode,
                m.AudienceType,
                m.StartDatetime.ToString("yyyy-MM-dd HH:mm:ss"),
                m.DurationMinutes,
                m.Location  ?? "-",
                m.ZoomJoinUrl   ?? "-",
                m.Status,
                m.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                m.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return ExcelExportHelper.GenerateExcel("Meetings", headers, rows);
        }

        // ── Mapper ───────────────────────────────────────────────────
        private static MeetingResponse MapToResponse(SqlDataReader reader)
        {
            return new MeetingResponse
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Mode = reader.GetString(reader.GetOrdinal("Mode")),
                AudienceType = reader.GetString(reader.GetOrdinal("AudienceType")),
                StartDatetime = reader.GetDateTime(reader.GetOrdinal("StartDatetime")),
                DurationMinutes = reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
                EndDatetime = reader.IsDBNull(reader.GetOrdinal("EndDatetime"))
            ? default
            : reader.GetDateTime(reader.GetOrdinal("EndDatetime")),

                InviteScheduledFor = reader.IsDBNull(reader.GetOrdinal("InvitesScheduledFor"))
            ? default
            : reader.GetDateTime(reader.GetOrdinal("InvitesScheduledFor")),
                InviteStatus = reader.GetInt16(reader.GetOrdinal("InviteStatus")),
                InvitesSentAt = reader.IsDBNull(reader.GetOrdinal("InvitesSentAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("InvitesSentAt")),
                Location = reader.IsDBNull(reader.GetOrdinal("Location"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Location")),
                ZoomJoinUrl = reader.IsDBNull(reader.GetOrdinal("ZoomJoinUrl"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ZoomJoinUrl")), 
                ZoomMeetingId = reader.IsDBNull(reader.GetOrdinal("ZoomMeetingId"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ZoomMeetingId")),
                ZoomStartUrl = reader.IsDBNull(reader.GetOrdinal("ZoomStartUrl"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ZoomStartUrl")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
               

            };
        }

        public async Task<List<int>> GetMeetingsDueForInviteSendAsync()
        {
            var ids = new List<int>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetMeetingsDueForInviteSend", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt32(reader.GetOrdinal("Id")));
            }

            return ids;
        }

        public async Task MarkInviteProcessingAsync(int meetingId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_MarkMeetingInviteProcessing", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task MarkInviteSentAsync(int meetingId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_MarkMeetingInviteSent", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task MarkInviteFailedAsync(int meetingId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_MarkMeetingInviteFailed", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MeetingId", meetingId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}