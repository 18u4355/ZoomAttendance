using System.ComponentModel.DataAnnotations.Schema;
using ZoomAttendance.Entities;

namespace ZoomAttendance.Models.Entities
{
    [Table("meetings")]
    public class Meeting
    {
        [Column("meeting_id")]
        public int MeetingId { get; set; }

        [Column("title")]
        public string Title { get; set; } = null!;

        [Column("zoom_url")]
        public string ZoomUrl { get; set; } = null!;

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("created_by")]
        public int CreatedBy { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }

        [Column("is_closed")]
        public bool IsClosed { get; set; }

        public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
    }
}