namespace ZoomAttendance.Models.ResponseModels
{
    public class AttendanceHistoryResponse
    {
        public string MeetingName { get; set; } = string.Empty;
        public DateTime MeetingDate { get; set; }
        public bool IsPresent { get; set; }
    }

    public class StaffAttendanceHistoryResponse
    {
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int TotalMeetings { get; set; }
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public List<AttendanceHistoryResponse> History { get; set; } = new();
    }
}