namespace ZoomAttendance.Models.ResponseModels
{
    public class QrCodeEmailResult
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Sent { get; set; }
        public string? FailureReason { get; set; }
    }
}