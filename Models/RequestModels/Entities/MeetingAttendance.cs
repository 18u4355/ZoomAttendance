using System.ComponentModel.DataAnnotations.Schema;

namespace ZoomAttendance.Models.Entities
{
    [Table("attendance")]
    public class MeetingAttendance
    {
        [Column("attendance_id")]
        public int AttendanceId { get; set; }

        [Column("meeting_id")]
        public int MeetingId { get; set; }

        [Column("join_time")]
        public DateTime? JoinTime { get; set; }

        [Column("confirm_attendance")]
        public bool ConfirmAttendance { get; set; } = false;

        [Column("confirmation_time")]
        public DateTime? ConfirmationTime { get; set; }

        [Column("confirmation_token")]
        public string? ConfirmationToken { get; set; }

        [Column("join_token")]
        public string? JoinToken { get; set; }

        [Column("staff_email")]
        public string StaffEmail { get; set; } = null!;
        [Column("staff_name")]
        public string? StaffName { get; set; } = null!;

        [Column("confirmation_expires_at")]
        public DateTime? ConfirmationExpiresAt { get; set; }

        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        public Meeting? Meeting { get; set; }

        [Column("attendance_channel")]
        public AttendanceChannel? AttendedVia { get; set; }

    }
}
