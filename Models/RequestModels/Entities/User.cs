using System.ComponentModel.DataAnnotations.Schema;

namespace ZoomAttendance.Models.Entities
{
    [Table("users")]
    public class User
    {
        public int UserId { get; set; }   // user_id
        public string StaffName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? PasswordHash { get; set; } = null!;
    }
}
