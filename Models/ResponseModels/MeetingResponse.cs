namespace ZoomAttendance.Models.ResponseModels
{
    public class MeetingResponse
    {
        public int MeetingId { get; set; } 
        public string Title { get; set; }

        public string ZoomUrl { get; set; }
        public bool IsActive { get; set; }
        public int TotalInvited { get; set; } 
        public int TotalJoined { get; set; }
        public int TotalConfirmed { get; set; }
        public bool IsClosed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
