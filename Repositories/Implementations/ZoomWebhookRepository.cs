using Microsoft.Data.SqlClient;
using System.Data;
using ZoomAttendance.Models;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Repositories.Implementations
{
    public class ZoomWebhookRepository : IZoomWebhookRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<ZoomWebhookRepository> _logger;

        public ZoomWebhookRepository(IConfiguration configuration, ILogger<ZoomWebhookRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        public async Task ProcessParticipantEventAsync(ZoomParticipantWebhookEvent webhookEvent, string rawPayload)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var eventId = await SaveWebhookEventAsync(connection, webhookEvent, rawPayload);
            var context = await ResolveContextAsync(connection, webhookEvent);

            if (!context.MeetingId.HasValue)
            {
                _logger.LogWarning("Zoom webhook event {EventName} could not resolve meeting for ZoomMeetingId {ZoomMeetingId}", webhookEvent.EventName, webhookEvent.ZoomMeetingId);
                return;
            }

            if (string.Equals(webhookEvent.EventName, "meeting.participant_joined", StringComparison.OrdinalIgnoreCase))
            {
                await HandleParticipantJoinedAsync(connection, webhookEvent, context, eventId);
            }
            else if (string.Equals(webhookEvent.EventName, "meeting.participant_left", StringComparison.OrdinalIgnoreCase))
            {
                await HandleParticipantLeftAsync(connection, webhookEvent, context, eventId);
            }

            if (context.StaffId.HasValue)
            {
                await RecalculateVirtualAttendanceAsync(connection, context.MeetingId.Value, context.StaffId.Value);
            }
        }

        private async Task<long> SaveWebhookEventAsync(SqlConnection connection, ZoomParticipantWebhookEvent webhookEvent, string rawPayload)
        {
            const string sql = @"
INSERT INTO dbo.ZoomWebhookEvents
(
    EventName,
    EventTs,
    ZoomMeetingId,
    ZoomMeetingUuid,
    ParticipantUserId,
    ParticipantUuid,
    RegistrantId,
    ParticipantEmail,
    ParticipantName,
    PayloadJson
)
OUTPUT INSERTED.Id
VALUES
(
    @EventName,
    @EventTs,
    @ZoomMeetingId,
    @ZoomMeetingUuid,
    @ParticipantUserId,
    @ParticipantUuid,
    @RegistrantId,
    @ParticipantEmail,
    @ParticipantName,
    @PayloadJson
);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@EventName", webhookEvent.EventName);
            command.Parameters.AddWithValue("@EventTs", (object?)webhookEvent.EventTimeUtc ?? DBNull.Value);
            command.Parameters.AddWithValue("@ZoomMeetingId", webhookEvent.ZoomMeetingId);
            command.Parameters.AddWithValue("@ZoomMeetingUuid", (object?)webhookEvent.ZoomMeetingUuid ?? DBNull.Value);
            command.Parameters.AddWithValue("@ParticipantUserId", (object?)webhookEvent.ParticipantUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ParticipantUuid", (object?)webhookEvent.ParticipantUuid ?? DBNull.Value);
            command.Parameters.AddWithValue("@RegistrantId", (object?)webhookEvent.RegistrantId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ParticipantEmail", (object?)webhookEvent.ParticipantEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@ParticipantName", (object?)webhookEvent.ParticipantName ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayloadJson", rawPayload);

            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }

        private async Task<ResolvedWebhookContext> ResolveContextAsync(SqlConnection connection, ZoomParticipantWebhookEvent webhookEvent)
        {
            const string meetingSql = @"
SELECT TOP 1 Id
FROM dbo.Meetings
WHERE ZoomMeetingId = @ZoomMeetingId;";

            int? meetingId;
            using (var meetingCommand = new SqlCommand(meetingSql, connection))
            {
                meetingCommand.Parameters.AddWithValue("@ZoomMeetingId", webhookEvent.ZoomMeetingId);
                var result = await meetingCommand.ExecuteScalarAsync();
                meetingId = result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
            }

            if (!meetingId.HasValue)
                return new ResolvedWebhookContext();

            Guid? staffId = null;

            if (!string.IsNullOrWhiteSpace(webhookEvent.RegistrantId))
            {
                const string registrantSql = @"
SELECT TOP 1 StaffId
FROM dbo.MeetingInvites
WHERE MeetingId = @MeetingId
  AND ZoomRegistrantId = @ZoomRegistrantId;";

                using var registrantCommand = new SqlCommand(registrantSql, connection);
                registrantCommand.Parameters.AddWithValue("@MeetingId", meetingId.Value);
                registrantCommand.Parameters.AddWithValue("@ZoomRegistrantId", webhookEvent.RegistrantId);
                var registrantResult = await registrantCommand.ExecuteScalarAsync();
                if (registrantResult != null && registrantResult != DBNull.Value)
                    staffId = (Guid)registrantResult;
            }

            if (!staffId.HasValue && !string.IsNullOrWhiteSpace(webhookEvent.ParticipantEmail))
            {
                const string emailSql = @"
SELECT TOP 1 a.StaffId
FROM dbo.Attendance a
INNER JOIN dbo.Staff s ON s.Id = a.StaffId
WHERE a.MeetingId = @MeetingId
  AND LOWER(s.Email) = LOWER(@Email);";

                using var emailCommand = new SqlCommand(emailSql, connection);
                emailCommand.Parameters.AddWithValue("@MeetingId", meetingId.Value);
                emailCommand.Parameters.AddWithValue("@Email", webhookEvent.ParticipantEmail);
                var emailResult = await emailCommand.ExecuteScalarAsync();
                if (emailResult != null && emailResult != DBNull.Value)
                    staffId = (Guid)emailResult;
            }

            return new ResolvedWebhookContext
            {
                MeetingId = meetingId,
                StaffId = staffId
            };
        }

        private async Task HandleParticipantJoinedAsync(SqlConnection connection, ZoomParticipantWebhookEvent webhookEvent, ResolvedWebhookContext context, long eventId)
        {
            const string existingSql = @"
SELECT TOP 1 Id
FROM dbo.ZoomParticipantSessions
WHERE MeetingId = @MeetingId
  AND LeftAt IS NULL
  AND (
        (@StaffId IS NOT NULL AND StaffId = @StaffId)
        OR (@StaffId IS NULL AND @RegistrantId IS NOT NULL AND RegistrantId = @RegistrantId)
        OR (@StaffId IS NULL AND @ParticipantEmail IS NOT NULL AND ParticipantEmail = @ParticipantEmail)
      )
ORDER BY JoinedAt DESC;";

            using (var existingCommand = new SqlCommand(existingSql, connection))
            {
                existingCommand.Parameters.AddWithValue("@MeetingId", context.MeetingId!.Value);
                existingCommand.Parameters.AddWithValue("@StaffId", (object?)context.StaffId ?? DBNull.Value);
                existingCommand.Parameters.AddWithValue("@RegistrantId", (object?)webhookEvent.RegistrantId ?? DBNull.Value);
                existingCommand.Parameters.AddWithValue("@ParticipantEmail", (object?)webhookEvent.ParticipantEmail ?? DBNull.Value);

                var existingId = await existingCommand.ExecuteScalarAsync();
                if (existingId != null && existingId != DBNull.Value)
                    return;
            }

            const string insertSql = @"
INSERT INTO dbo.ZoomParticipantSessions
(
    MeetingId,
    StaffId,
    ZoomMeetingId,
    ZoomMeetingUuid,
    RegistrantId,
    ParticipantUserId,
    ParticipantUuid,
    ParticipantEmail,
    ParticipantName,
    JoinedAt,
    SourceEventJoinId
)
VALUES
(
    @MeetingId,
    @StaffId,
    @ZoomMeetingId,
    @ZoomMeetingUuid,
    @RegistrantId,
    @ParticipantUserId,
    @ParticipantUuid,
    @ParticipantEmail,
    @ParticipantName,
    @JoinedAt,
    @SourceEventJoinId
);";

            using var insertCommand = new SqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@MeetingId", context.MeetingId!.Value);
            insertCommand.Parameters.AddWithValue("@StaffId", (object?)context.StaffId ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@ZoomMeetingId", webhookEvent.ZoomMeetingId);
            insertCommand.Parameters.AddWithValue("@ZoomMeetingUuid", (object?)webhookEvent.ZoomMeetingUuid ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@RegistrantId", (object?)webhookEvent.RegistrantId ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@ParticipantUserId", (object?)webhookEvent.ParticipantUserId ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@ParticipantUuid", (object?)webhookEvent.ParticipantUuid ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@ParticipantEmail", (object?)webhookEvent.ParticipantEmail ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@ParticipantName", (object?)webhookEvent.ParticipantName ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@JoinedAt", webhookEvent.OccurredAtUtc ?? webhookEvent.EventTimeUtc ?? DateTime.UtcNow);
            insertCommand.Parameters.AddWithValue("@SourceEventJoinId", eventId);
            await insertCommand.ExecuteNonQueryAsync();
        }

        private async Task HandleParticipantLeftAsync(SqlConnection connection, ZoomParticipantWebhookEvent webhookEvent, ResolvedWebhookContext context, long eventId)
        {
            const string openSessionSql = @"
SELECT TOP 1 Id, JoinedAt
FROM dbo.ZoomParticipantSessions
WHERE MeetingId = @MeetingId
  AND LeftAt IS NULL
  AND (
        (@StaffId IS NOT NULL AND StaffId = @StaffId)
        OR (@StaffId IS NULL AND @RegistrantId IS NOT NULL AND RegistrantId = @RegistrantId)
        OR (@StaffId IS NULL AND @ParticipantEmail IS NOT NULL AND ParticipantEmail = @ParticipantEmail)
      )
ORDER BY JoinedAt DESC;";

            long? sessionId = null;
            DateTime? joinedAt = null;

            using (var openSessionCommand = new SqlCommand(openSessionSql, connection))
            {
                openSessionCommand.Parameters.AddWithValue("@MeetingId", context.MeetingId!.Value);
                openSessionCommand.Parameters.AddWithValue("@StaffId", (object?)context.StaffId ?? DBNull.Value);
                openSessionCommand.Parameters.AddWithValue("@RegistrantId", (object?)webhookEvent.RegistrantId ?? DBNull.Value);
                openSessionCommand.Parameters.AddWithValue("@ParticipantEmail", (object?)webhookEvent.ParticipantEmail ?? DBNull.Value);

                using var reader = await openSessionCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    sessionId = reader.GetInt64(reader.GetOrdinal("Id"));
                    joinedAt = reader.GetDateTime(reader.GetOrdinal("JoinedAt"));
                }
            }

            var leftAt = webhookEvent.OccurredAtUtc ?? webhookEvent.EventTimeUtc ?? DateTime.UtcNow;

            if (sessionId.HasValue && joinedAt.HasValue)
            {
                const string updateSql = @"
UPDATE dbo.ZoomParticipantSessions
SET LeftAt = @LeftAt,
    DurationMinutes = CASE WHEN DATEDIFF(MINUTE, JoinedAt, @LeftAt) < 0 THEN 0 ELSE DATEDIFF(MINUTE, JoinedAt, @LeftAt) END,
    SourceEventLeftId = @SourceEventLeftId,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

                using var updateCommand = new SqlCommand(updateSql, connection);
                updateCommand.Parameters.AddWithValue("@LeftAt", leftAt);
                updateCommand.Parameters.AddWithValue("@SourceEventLeftId", eventId);
                updateCommand.Parameters.AddWithValue("@Id", sessionId.Value);
                await updateCommand.ExecuteNonQueryAsync();
                return;
            }

            const string insertSql = @"
INSERT INTO dbo.ZoomParticipantSessions
(
    MeetingId,
    StaffId,
    ZoomMeetingId,
    ZoomMeetingUuid,
    RegistrantId,
    ParticipantUserId,
    ParticipantUuid,
    ParticipantEmail,
    ParticipantName,
    JoinedAt,
    LeftAt,
    DurationMinutes,
    SourceEventLeftId
)
VALUES
(
    @MeetingId,
    @StaffId,
    @ZoomMeetingId,
    @ZoomMeetingUuid,
    @RegistrantId,
    @ParticipantUserId,
    @ParticipantUuid,
    @ParticipantEmail,
    @ParticipantName,
    @JoinedAt,
    @LeftAt,
    0,
    @SourceEventLeftId
);";

            using var insertCommand = new SqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@MeetingId", context.MeetingId!.Value);
            insertCommand.Parameters.AddWithValue("@StaffId", (object?)context.StaffId ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@ZoomMeetingId", webhookEvent.ZoomMeetingId);
            insertCommand.Parameters.AddWithValue("@ZoomMeetingUuid", (object?)webhookEvent.ZoomMeetingUuid ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@RegistrantId", (object?)webhookEvent.RegistrantId ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@ParticipantUserId", (object?)webhookEvent.ParticipantUserId ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@ParticipantUuid", (object?)webhookEvent.ParticipantUuid ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@ParticipantEmail", (object?)webhookEvent.ParticipantEmail ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@ParticipantName", (object?)webhookEvent.ParticipantName ?? DBNull.Value);
            insertCommand.Parameters.AddWithValue("@JoinedAt", leftAt);
            insertCommand.Parameters.AddWithValue("@LeftAt", leftAt);
            insertCommand.Parameters.AddWithValue("@SourceEventLeftId", eventId);
            await insertCommand.ExecuteNonQueryAsync();
        }

        private async Task RecalculateVirtualAttendanceAsync(SqlConnection connection, int meetingId, Guid staffId)
        {
            const string aggregateSql = @"
SELECT
    MIN(zps.JoinedAt) AS FirstJoinedAt,
    SUM(ISNULL(zps.DurationMinutes, 0)) AS TotalMinutes,
    COALESCE(NULLIF(m.VirtualAttendanceThresholdMinutes, 0), m.DurationMinutes, 0) AS ThresholdMinutes
FROM dbo.Meetings m
LEFT JOIN dbo.ZoomParticipantSessions zps
    ON zps.MeetingId = m.Id
   AND zps.StaffId = @StaffId
WHERE m.Id = @MeetingId
GROUP BY m.VirtualAttendanceThresholdMinutes, m.DurationMinutes;";

            DateTime? firstJoinedAt = null;
            var totalMinutes = 0;
            var thresholdMinutes = 0;

            using (var aggregateCommand = new SqlCommand(aggregateSql, connection))
            {
                aggregateCommand.Parameters.AddWithValue("@MeetingId", meetingId);
                aggregateCommand.Parameters.AddWithValue("@StaffId", staffId);

                using var reader = await aggregateCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    firstJoinedAt = reader.IsDBNull(reader.GetOrdinal("FirstJoinedAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("FirstJoinedAt"));
                    totalMinutes = reader.IsDBNull(reader.GetOrdinal("TotalMinutes"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("TotalMinutes"));
                    thresholdMinutes = reader.IsDBNull(reader.GetOrdinal("ThresholdMinutes"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("ThresholdMinutes"));
                }
            }

            const string updateAttendanceSql = @"
UPDATE dbo.Attendance
SET JoinedAt = @JoinedAt,
    ConfirmedAt = NULL,
    VirtualAttendanceMinutes = @VirtualAttendanceMinutes,
    AttendanceThresholdMinutes = @AttendanceThresholdMinutes,
    AttendanceEvaluatedAt = SYSUTCDATETIME(),
    Status = CASE
                 WHEN @VirtualAttendanceMinutes >= @AttendanceThresholdMinutes AND @AttendanceThresholdMinutes > 0 THEN 'present'
                 ELSE 'absent'
             END,
    UpdatedAt = SYSUTCDATETIME()
WHERE MeetingId = @MeetingId
  AND StaffId = @StaffId;";

            using var updateCommand = new SqlCommand(updateAttendanceSql, connection);
            updateCommand.Parameters.AddWithValue("@JoinedAt", (object?)firstJoinedAt ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@VirtualAttendanceMinutes", totalMinutes);
            updateCommand.Parameters.AddWithValue("@AttendanceThresholdMinutes", thresholdMinutes);
            updateCommand.Parameters.AddWithValue("@MeetingId", meetingId);
            updateCommand.Parameters.AddWithValue("@StaffId", staffId);
            await updateCommand.ExecuteNonQueryAsync();
        }

        private sealed class ResolvedWebhookContext
        {
            public int? MeetingId { get; set; }
            public Guid? StaffId { get; set; }
        }
    }
}
