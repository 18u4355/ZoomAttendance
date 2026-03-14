// Entities/MeetingInvite.cs

namespace ZoomAttendance.Entities
{
    public class MeetingInvite
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public Guid StaffId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; }
        public DateTime? ResentAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? JoinedAt { get; set; }
        public string? AttendanceMode { get; set; }
        public decimal? StaffLatitude { get; set; }
        public decimal? StaffLongitude { get; set; }
        public bool? IsWithinFence { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}