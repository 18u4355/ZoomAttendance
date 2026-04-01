// Models/RequestModels/StaffAttendanceReportRequest.cs

namespace ZoomAttendance.Models.RequestModels
{
    public class StaffAttendanceReportRequest
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}