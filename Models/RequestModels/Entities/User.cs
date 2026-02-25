using System.ComponentModel.DataAnnotations.Schema;

namespace ZoomAttendance.Models.Entities
{
    [Table("users")]
    public class User
    {
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("staff_name")]
        public string StaffName { get; set; } = null!;

        [Column("email")]
        public string Email { get; set; } = null!;

        [Column("role")]
        public string Role { get; set; } = null!;

        [Column("department")]
        public string? Department { get; set; }

        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}