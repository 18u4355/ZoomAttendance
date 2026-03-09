// Entities/Attendance.cs

namespace ZoomAttendance.Entities
{
    public class Attendance
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public int StaffId { get; set; }
        public string Mode { get; set; } = string.Empty;
        public string Status { get; set; } = "absent";
        public DateTime? CheckInAt { get; set; }
        public decimal? CheckInLat { get; set; }
        public decimal? CheckInLng { get; set; }
        public bool? CheckInWithinFence { get; set; }
        public DateTime? CheckOutAt { get; set; }
        public decimal? CheckOutLat { get; set; }
        public decimal? CheckOutLng { get; set; }
        public bool? CheckOutWithinFence { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? CheckOutToken { get; set; }
        public string? EndConfirmToken { get; set; }
        public bool IsLate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}