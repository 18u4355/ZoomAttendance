// Models/ResponseModels/AttendanceResponse.cs
// StaffId changed to Guid

namespace ZoomAttendance.Models.ResponseModels
{
    public class AttendanceResponse
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public string MeetingTitle { get; set; } = string.Empty;
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string StaffEmail { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? CheckInAt { get; set; }
        public bool? CheckInWithinFence { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PagedAttendanceResponse
    {
        public List<AttendanceResponse> Data { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalPages { get; set; }
    }

    public class AttendanceSummaryResponse
    {
        public int Total { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int LeftEarly { get; set; }
        public int Joined { get; set; }
        public int CheckedIn { get; set; }
        public double AttendanceRate => Total == 0 ? 0 : Math.Round((double)Present / Total * 100, 1);
    }

    public class CheckInResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class VirtualJoinResponse
    {
        public string ZoomJoinUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class PendingVirtualConfirm
    {
        public int MeetingId { get; set; }
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string StaffEmail { get; set; } = string.Empty;
        public string MeetingTitle { get; set; } = string.Empty;
        public string EndConfirmToken { get; set; } = string.Empty;
    }
}