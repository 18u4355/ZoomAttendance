// Models/ResponseModels/BulkUploadResponse.cs
// StaffId changed to Guid?

namespace ZoomAttendance.Models.ResponseModels
{
    public class BulkUploadResponse
    {
        public int TotalRows { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public List<BulkUploadRowResult> Results { get; set; } = new();
    }

    public class BulkUploadRowResult
    {
        public int RowNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid? StaffId { get; set; }
    }
}