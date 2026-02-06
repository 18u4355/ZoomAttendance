namespace ZoomAttendance.Models.RequestModels
{
    public class StaffJoinRequest
    {
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}
