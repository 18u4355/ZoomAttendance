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

            // Newer proc versions may return a second, richer meeting result set
            // before the departments result set. We consume whichever comes next.
            while (await reader.NextResultAsync())
            {
                if (ResultSetHasColumn(reader, "AudienceType"))
                {
                    if (await reader.ReadAsync())
                        meeting = MapToResponse(reader);

                    continue;
                }

                if (ResultSetHasColumn(reader, "Name") && !ResultSetHasColumn(reader, "Mode"))
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
            }

            return meeting;
        }
        public async Task<MeetingResponse> CreateAsync(CreateMeetingRequest request)
        {
            var mode = request.Mode.ToLower().Trim();
            var audienceType = request.AudienceType.ToLower().Trim();

            ValidateMeetingModeRules(mode, request.VirtualAttendanceThresholdMinutes);
            ValidateMeetingSchedule(request.StartDatetime);
            await ValidateMeetingConfigurationAsync(
                mode,
                audienceType,
                request.DurationMinutes,
                request.VenueId,
                request.VirtualAttendanceThresholdMinutes,
                request.DepartmentIds,
                request.VirtualStaffIds);

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
            command.Parameters.AddWithValue("@VirtualAttendanceThresholdMinutes", (object?)request.VirtualAttendanceThresholdMinutes ?? DBNull.Value);
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

            ValidateMeetingCanBeEdited(existing);
            ValidateMeetingModeRules(mode, request.VirtualAttendanceThresholdMinutes);
            ValidateMeetingSchedule(request.StartDatetime);
            await ValidateMeetingConfigurationAsync(
                mode,
                audienceType,
                request.DurationMinutes,
                request.VenueId,
                request.VirtualAttendanceThresholdMinutes,
                request.DepartmentIds,
                request.VirtualStaffIds);

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
            command.Parameters.AddWithValue("@VirtualAttendanceThresholdMinutes", (object?)request.VirtualAttendanceThresholdMinutes ?? DBNull.Value);
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
                    VirtualAttendanceThresholdMinutes = GetNullableInt(reader, "VirtualAttendanceThresholdMinutes"),
                    VenueId = GetNullableInt(reader, "VenueId"),
                    VenueName = GetNullableString(reader, "VenueName") ?? GetNullableString(reader, "Location"),
                    ZoomJoinUrl = reader.IsDBNull(reader.GetOrdinal("ZoomJoinUrl")) ? null : reader.GetString(reader.GetOrdinal("ZoomJoinUrl")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                });
            }

            var headers = new[]
            {
                "Id", "Title", "Mode", "Audience", "Start Date", "Duration (mins)",
                "Venue", "Zoom URL", "Status", "Created At", "Updated At"
            };

            var rows = records.Select(m => new List<object?>
            {
                m.Id,
                m.Title,
                m.Mode,
                m.AudienceType,
                m.StartDatetime.ToString("yyyy-MM-dd HH:mm:ss"),
                m.DurationMinutes,
                m.VenueName ?? "-",
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
                VirtualAttendanceThresholdMinutes = GetNullableInt(reader, "VirtualAttendanceThresholdMinutes"),
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
                VenueId = GetNullableInt(reader, "VenueId"),
                VenueName = GetNullableString(reader, "VenueName") ?? GetNullableString(reader, "Location"),
                VenueLatitude = GetNullableDecimal(reader, "VenueLatitude"),
                VenueLongitude = GetNullableDecimal(reader, "VenueLongitude"),
                VenueRadiusMetres = GetNullableInt(reader, "VenueRadiusMetres"),
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

        private static string? GetNullableString(SqlDataReader reader, string columnName)
        {
            var ordinal = TryGetOrdinal(reader, columnName);
            return ordinal.HasValue && !reader.IsDBNull(ordinal.Value)
                ? reader.GetString(ordinal.Value)
                : null;
        }

        private static int? GetNullableInt(SqlDataReader reader, string columnName)
        {
            var ordinal = TryGetOrdinal(reader, columnName);
            return ordinal.HasValue && !reader.IsDBNull(ordinal.Value)
                ? reader.GetInt32(ordinal.Value)
                : null;
        }

        private static decimal? GetNullableDecimal(SqlDataReader reader, string columnName)
        {
            var ordinal = TryGetOrdinal(reader, columnName);
            return ordinal.HasValue && !reader.IsDBNull(ordinal.Value)
                ? reader.GetDecimal(ordinal.Value)
                : null;
        }

        private static int? TryGetOrdinal(SqlDataReader reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return null;
        }

        private static bool ResultSetHasColumn(SqlDataReader reader, string columnName)
        {
            return TryGetOrdinal(reader, columnName).HasValue;
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

        public async Task UpdateMeetingStatusesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_UpdateMeetingStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        private static void ValidateMeetingSchedule(DateTime startDatetime)
        {
            if (startDatetime.ToUniversalTime() <= DateTime.UtcNow)
                throw new InvalidOperationException("Meeting start time cannot be in the past.");
        }

        private static void ValidateMeetingModeRules(string mode, int? virtualAttendanceThresholdMinutes)
        {
            if (mode == "physical" && virtualAttendanceThresholdMinutes.HasValue)
                throw new InvalidOperationException("Virtual attendance threshold cannot be set for a physical meeting.");
        }

        private async Task ValidateMeetingConfigurationAsync(
            string mode,
            string audienceType,
            int durationMinutes,
            int? venueId,
            int? virtualAttendanceThresholdMinutes,
            List<int>? departmentIds,
            List<Guid>? virtualStaffIds)
        {
            if (mode == "virtual" && venueId.HasValue)
                throw new InvalidOperationException("Venue must not be provided for a virtual meeting.");

            if ((mode == "physical" || mode == "hybrid") && !venueId.HasValue)
                throw new InvalidOperationException("Venue is required for physical or hybrid meetings.");

            if ((mode == "virtual" || mode == "hybrid") && !virtualAttendanceThresholdMinutes.HasValue)
                throw new InvalidOperationException("Virtual attendance threshold is required for virtual or hybrid meetings.");

            if ((mode == "virtual" || mode == "hybrid") &&
                virtualAttendanceThresholdMinutes.HasValue &&
                virtualAttendanceThresholdMinutes.Value > durationMinutes)
            {
                throw new InvalidOperationException("Virtual attendance threshold cannot be greater than meeting duration.");
            }

            if (audienceType == "all_staff" && departmentIds != null && departmentIds.Count > 0)
                throw new InvalidOperationException("Department IDs must not be provided when audience type is all_staff.");

            if (audienceType == "departments" && (departmentIds == null || departmentIds.Count == 0))
                throw new InvalidOperationException("At least one department must be provided when audience type is departments.");

            if (mode == "hybrid")
            {

                if (virtualStaffIds == null || virtualStaffIds.Count == 0)
                    throw new InvalidOperationException("Virtual staff must be provided for a hybrid meeting.");
            }

            if (departmentIds != null && departmentIds.Count > 0)
            {
                var distinctDepartmentIds = departmentIds.Distinct().ToList();

                using var connection = new SqlConnection(_connectionString);
                const string sql = @"
                        SELECT COUNT(*)
                        FROM dbo.Departments
                        WHERE Id IN ({0});";

                var parameterNames = distinctDepartmentIds.Select((_, index) => $"@DepartmentId{index}").ToList();
                using var command = new SqlCommand(string.Format(sql, string.Join(", ", parameterNames)), connection);

                for (var i = 0; i < distinctDepartmentIds.Count; i++)
                    command.Parameters.AddWithValue(parameterNames[i], distinctDepartmentIds[i]);

                await connection.OpenAsync();
                var count = (int)await command.ExecuteScalarAsync();

                if (count != distinctDepartmentIds.Count)
                    throw new InvalidOperationException("One or more selected departments do not exist.");
            }
        }

        private static void ValidateMeetingCanBeEdited(MeetingResponse meeting)
        {
            if (meeting.StartDatetime.ToUniversalTime() <= DateTime.UtcNow)
                throw new InvalidOperationException("You can only edit a meeting before it starts.");

            if (string.Equals(meeting.Status, "in_progress", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(meeting.Status, "completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(meeting.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("This meeting can no longer be edited.");
            }
        }
    }
}
