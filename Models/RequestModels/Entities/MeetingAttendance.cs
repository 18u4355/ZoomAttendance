using System.ComponentModel.DataAnnotations.Schema;

namespace ZoomAttendance.Models.Entities
{
    [Table("attendance")]
    public class MeetingAttendance
    {
        public int AttendanceId { get; set; }      // attendance_id
        public int MeetingId { get; set; }
        public DateTime? JoinTime { get; set; }   
        public bool ConfirmAttendance { get; set; } = false;
        public DateTime? ConfirmationTime { get; set; }   
        public string? ConfirmationToken { get; set; }
        public string? JoinToken { get; set; }
        public string StaffEmail { get; set; } = null!;
        public string StaffName { get; set; } = null!; // required
        public DateTime? ConfirmationExpiresAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? CreatedAt { get; set; }


        public Meeting? Meeting { get; set; }

        public User User { get; set; }
    }
}
