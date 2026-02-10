using System.ComponentModel.DataAnnotations.Schema;

namespace ZoomAttendance.Models.Entities
{
    [Table("attendance")]
    public class MeetingAttendance
    {
        public int AttendanceId { get; set; }      // attendance_id
        public int MeetingId { get; set; }
        public DateTime? JoinTime { get; set; }   // nullable now
        public bool ConfirmAttendance { get; set; } = false;
        public DateTime? ConfirmationTime { get; set; }
        public string? ConfirmationToken { get; set; }
        public string? JoinToken { get; set; }
        public string StaffEmail { get; set; } = null!; // required
        public DateTime? ConfirmationExpiresAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public Meeting? Meeting { get; set; }


    }
}
