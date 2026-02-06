namespace ZoomAttendance.Models.ResponseModels
{
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Token { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
    
    }
}
