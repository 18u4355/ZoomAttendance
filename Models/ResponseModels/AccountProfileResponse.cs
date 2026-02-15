namespace ZoomAttendance.Models.ResponseModels
{
    public class AccountProfileResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}