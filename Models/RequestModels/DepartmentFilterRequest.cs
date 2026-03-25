// Models/RequestModels/DepartmentFilterRequest.cs

namespace ZoomAttendance.Models.RequestModels
{
    public class DepartmentFilterRequest
    {
        public bool IncludeInactive { get; set; } = false;
    }
}