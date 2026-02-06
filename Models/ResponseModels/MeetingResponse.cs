namespace ZoomAttendance.Models.ResponseModels
{
    public class MeetingResponse
    {
        public int MeetingId { get; set; }
        public string Title { get; set; } = null!;
        public string ZoomUrl { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public bool IsActive { get; set; }
    }
}
