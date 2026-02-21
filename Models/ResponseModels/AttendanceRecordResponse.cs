namespace ZoomAttendance.Models.ResponseModels
{
    public class AttendanceRecordResponse
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; }
    }
}
