// Entities/Meeting.cs

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
        public string? Location { get; set; }
        public string? ZoomUrl { get; set; }
        public string Status { get; set; } = "scheduled";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}