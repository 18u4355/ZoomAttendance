namespace ZoomAttendance.Models.RequestModels
{
    public class AttendanceReportRequest
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? MeetingTitle { get; set; }
        public string? StaffEmail { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool ExportCsv { get; set; } = false; // optional
    }
}
