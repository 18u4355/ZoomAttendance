namespace ZoomAttendance.Models
{
    public class ZoomParticipantWebhookEvent
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime? EventTimeUtc { get; set; }
        public string ZoomMeetingId { get; set; } = string.Empty;
        public string? ZoomMeetingUuid { get; set; }
        public string? ParticipantUserId { get; set; }
        public string? ParticipantUuid { get; set; }
        public string? RegistrantId { get; set; }
        public string? ParticipantEmail { get; set; }
        public string? ParticipantName { get; set; }
        public DateTime? OccurredAtUtc { get; set; }
    }
}
