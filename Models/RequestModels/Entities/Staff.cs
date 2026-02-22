using System.ComponentModel.DataAnnotations.Schema;

namespace ZoomAttendance.Entities
{
    [Table("staff")]
    public class Staff
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("department")]
        public string Department { get; set; } = string.Empty;

        [Column("barcode_token")]
        public string BarcodeToken { get; set; } = string.Empty;

        [Column("pin_hash")]
        public string? PinHash { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
    }
}