namespace ZoomAttendance.Models.RequestModels
{
    public class CreateMeetingRequest
    {
        public string Title { get; set; } = null!;
        public string ZoomUrl { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
