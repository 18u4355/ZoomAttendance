// Models/RequestModels/AttendanceRequests.cs

using System.ComponentModel.DataAnnotations;

namespace ZoomAttendance.Models.RequestModels
{
    public class PhysicalCheckInRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [Range(-90, 90)]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public decimal Longitude { get; set; }
    }

    public class PhysicalCheckOutRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [Range(-90, 90)]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public decimal Longitude { get; set; }
    }

    public class VirtualJoinRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class VirtualEndConfirmRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class ManualStatusUpdateRequest
    {
        [Required]
        public int MeetingId { get; set; }

        [Required]
        public int StaffId { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class AttendanceFilterRequest
    {
        public string? MeetingTitle { get; set; }
        public string? StaffName { get; set; }
        public int? DepartmentId { get; set; }
        public string? Status { get; set; } 
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;
    }
}