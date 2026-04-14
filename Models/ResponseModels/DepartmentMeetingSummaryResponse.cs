namespace ZoomAttendance.Models.ResponseModels
{
    public class DepartmentMeetingSummaryResponse
    {
        public int DeptId { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public int StaffCount { get; set; }
        public int TotalMeetingsInvited { get; set; }
        public int TotalAttended { get; set; }
        public int TotalMissed { get; set; }
        public List<MeetingSummaryItem> Meetings { get; set; } = new();
    }

    public class MeetingSummaryItem
    {
        public int MeetingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public DateTime StartDatetime { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<MeetingStaffItem> Staff { get; set; } = new();
    }

    public class MeetingStaffItem
    {
        public Guid StaffId { get; set; }  
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AttendanceStatus { get; set; } = string.Empty;
    }
}