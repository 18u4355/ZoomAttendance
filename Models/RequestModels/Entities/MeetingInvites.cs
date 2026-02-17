using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZoomAttendance.Models.Entities
{
    [Table("meetinginvites")]
    public class MeetingInvites
    {
        [Key]

        [Column("meeting_id")]
        public int MeetingId { get; set; }

        [Required]
        [Column("email")]
        [MaxLength(150)]
        public string Email { get; set; }

        [Column("sent_date")]
        public DateTime? SentDate { get; set; }

        [Column("email_status")]
        [MaxLength(20)]
        public string? EmailStatus { get; set; }

        // Navigation property
        [ForeignKey("MeetingId")]
        public Meeting Meeting { get; set; }
    }
}