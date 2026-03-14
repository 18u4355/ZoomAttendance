namespace ZoomAttendance.Entities
{
    public class Meeting
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string AudienceType { get; set; } = string.Empty;
        public DateTime StartDatetime { get; set; }
        public int DurationMinutes { get; set; }

        public DateTime EndDatetime { get; set; }

        public DateTime InviteScheduledFor { get; set; }
        public string InviteStatus { get; set; } = "pending";
        public DateTime? InvitesSentAt { get; set; }

        public string? Location { get; set; }

        public string? ZoomJoinUrl { get; set; }
        public string? ZoomMeetingId { get; set; }
        public string? ZoomStartUrl { get; set; }

        public string Status { get; set; } = "scheduled";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}