namespace ZoomAttendance.Models.RequestModels
{
    public class InviteHrRequest
    {
        public string StaffName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Department { get; set; }
    }
}