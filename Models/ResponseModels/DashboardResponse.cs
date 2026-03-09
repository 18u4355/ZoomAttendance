// Models/ResponseModels/DashboardResponse.cs

namespace ZoomAttendance.Models.ResponseModels
{
    public class DashboardResponse
    {
        public DashboardCountsResponse Counts { get; set; } = new();
        public DashboardAttendanceResponse AttendanceSummary { get; set; } = new();
        public List<UpcomingMeetingResponse> UpcomingMeetings { get; set; } = new();
        public List<QuickActionResponse> QuickActions { get; set; } = new();
    }

    public class DashboardCountsResponse
    {
        public int TotalMeetings { get; set; }
        public int UpcomingMeetings { get; set; }
        public int TotalActiveStaff { get; set; }
        public int TotalDepartments { get; set; }
    }

    public class DashboardAttendanceResponse
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

    public class UpcomingMeetingResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDatetime { get; set; }
        public int DurationMinutes { get; set; }
        public string? Location { get; set; }
        public string? ZoomUrl { get; set; }
    }

    public class QuickActionResponse
    {
        public string Label { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
    }
}