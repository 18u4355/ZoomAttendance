namespace ZoomAttendance.Models.ResponseModels
{
    public class StaffEmailResponse
    {
        public int Id { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}