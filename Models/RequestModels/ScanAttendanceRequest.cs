using ZoomAttendance.Entities;

namespace ZoomAttendance.Models.RequestModels
{
    public class ScanAttendanceRequest
    {
        public string Token { get; set; } = string.Empty;
        public int MeetingId { get; set; }
    }
}