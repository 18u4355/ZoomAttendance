// Models/ResponseModels/MeetingResponse.cs

namespace ZoomAttendance.Models.ResponseModels
{
    public class MeetingDepartmentResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class MeetingResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string AudienceType { get; set; } = string.Empty;
        public DateTime StartDatetime { get; set; }
        public int DurationMinutes { get; set; }
        public string? Location { get; set; }
        public string? ZoomJoinUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<MeetingDepartmentResponse> Departments { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PagedMeetingResponse
    {
        public IEnumerable<MeetingResponse> Data { get; set; } = Enumerable.Empty<MeetingResponse>();
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / Limit);
    }
}