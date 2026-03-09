// Models/ResponseModels/StaffResponse.cs

namespace ZoomAttendance.Models.ResponseModels
{
    public class StaffResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PagedStaffResponse
    {
        public IEnumerable<StaffResponse> Data { get; set; } = Enumerable.Empty<StaffResponse>();
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / Limit);
    }
}