namespace ZoomAttendance.Models.ResponseModels
{
    public class DashboardSummaryResponse
    {
        public int TotalMeetings { get; set; }
        public int ActiveMeetings { get; set; }
        public int ClosedMeetings { get; set; }

        public int TotalInvited { get; set; }
        public int TotalConfirmed { get; set; }

    }
}
