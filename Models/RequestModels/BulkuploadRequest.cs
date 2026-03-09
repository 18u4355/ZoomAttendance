// Models/RequestModels/BulkUploadRequest.cs

namespace ZoomAttendance.Models.RequestModels
{
    public class BulkUploadStaffRow
    {
        public int RowNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
    }
}
