namespace ZoomAttendance.Models.ResponseModels
{
    public class AttendanceReportResponse
    {
        public int MeetingId { get; set; }
        public string? MeetingTitle { get; set; }

        public string StaffEmail { get; set; } = null!;

        public DateTime JoinTime { get; set; }

        public bool ConfirmedAttendance { get; set; }

        public DateTime? ConfirmationTime { get; set; }
    }
}
