namespace ZoomAttendance.Models.ResponseModels
{
    public class MeetingAttendanceResponsePhysical
    {
        public int MeetingId { get; set; }
        public string MeetingTitle { get; set; } = string.Empty;
        public DateTime MeetingDate { get; set; }
        public int TotalPhysicalAttendees { get; set; }
        public List<AttendanceRecordResponse> Attendees { get; set; } = new();
    }
}