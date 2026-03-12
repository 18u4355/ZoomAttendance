// Models/ResponseModels/DepartmentResponse.cs

namespace ZoomAttendance.Models.ResponseModels
{
    public class DepartmentResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StaffCount { get; set; }
        public int MeetingCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}