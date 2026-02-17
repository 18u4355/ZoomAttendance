using System.ComponentModel.DataAnnotations.Schema;

namespace ZoomAttendance.Models.Entities
{
    [Table("meetings")]
    public class Meeting
    {
        public int MeetingId { get; set; }

        public string? Title { get; set; }

        public string? ZoomUrl { get; set; }

        public int CreatedBy { get; set; }

        public bool IsActive { get; set; }

        public DateTime? ClosedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool? IsClosed { get; set; }
       // public DateTime? UpdatedAt { get; set; }

    }

}