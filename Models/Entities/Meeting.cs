using System.ComponentModel.DataAnnotations.Schema;

namespace ZoomAttendance.Models.Entities
{
    [Table("meetings")]
    public class Meeting
    {
        public int MeetingId { get; set; }
        public string Title { get; set; } = null!;
        public string ZoomUrl { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int CreatedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
