namespace ZoomAttendance.Models.RequestModels
{
    public class RegisterStaffRequest
    {
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}