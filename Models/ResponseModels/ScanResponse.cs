namespace ZoomAttendance.Models.ResponseModels
{
    public class ScanResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? StaffName { get; set; }
        public DateTime? ScannedAt { get; set; }
    }
}