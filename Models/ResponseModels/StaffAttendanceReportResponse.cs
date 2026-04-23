// Models/ResponseModels/StaffAttendanceReportResponse.cs

namespace ZoomAttendance.Models.ResponseModels
{
    public class StaffAttendanceReportResponse
    {
        // Staff info
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string StaffEmail { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string StaffStatus { get; set; } = string.Empty;

        // Summary
        public int TotalInvited { get; set; }
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalJoined { get; set; }
        public int TotalLate { get; set; }
        public double AttendanceRate { get; set; }

        // Meetings list
        public List<StaffMeetingRecord> Meetings { get; set; } = new();
    }

    public class StaffMeetingRecord
    {
        public int AttendanceId { get; set; }
        public int MeetingId { get; set; }
        public string MeetingTitle { get; set; } = string.Empty;
        public string MeetingMode { get; set; } = string.Empty;
        public string? VenueName { get; set; }
        public DateTime StartDatetime { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AttendanceMode { get; set; } = string.Empty;
        public DateTime? CheckInAt { get; set; }
        public bool? CheckInWithinFence { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
