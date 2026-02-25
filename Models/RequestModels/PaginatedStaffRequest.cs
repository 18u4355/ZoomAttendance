namespace ZoomAttendance.Models.RequestModels
{
    public class PaginatedStaffRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
    }
}