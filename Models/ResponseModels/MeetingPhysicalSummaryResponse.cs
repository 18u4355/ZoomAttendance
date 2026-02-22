namespace ZoomAttendance.Models.ResponseModels
{
    public class MeetingPhysicalSummaryResponse
    {
        public int MeetingId { get; set; }
        public string MeetingTitle { get; set; } = string.Empty;
        public DateTime MeetingDate { get; set; }
        public int TotalQrCodesSent { get; set; }
        public int TotalPhysicalAttendees { get; set; }
    }
}