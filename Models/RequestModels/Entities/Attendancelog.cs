using System.ComponentModel.DataAnnotations.Schema;
using ZoomAttendance.Models.Entities;

namespace ZoomAttendance.Entities
{
    public enum AttendanceType
    {
        Online,
        Physical
    }

    [Table("attendance_logs")]
    public class AttendanceLog
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("staff_id")]
        public int StaffId { get; set; }

        [Column("meeting_id")]
        public int MeetingId { get; set; }

        [Column("scanned_at")]
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

        public Staff Staff { get; set; } = null!;
        public Meeting Meeting { get; set; } = null!;
    }
}