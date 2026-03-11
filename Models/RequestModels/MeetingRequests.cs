// Models/RequestModels/MeetingRequests.cs

using System.ComponentModel.DataAnnotations;

namespace ZoomAttendance.Models.RequestModels
{
    public class CreateMeetingRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mode is required.")]
        public string Mode { get; set; } = string.Empty; // physical | virtual | hybrid

        [Required(ErrorMessage = "AudienceType is required.")]
        public string AudienceType { get; set; } = string.Empty; // all_staff | departments

        [Required(ErrorMessage = "StartDatetime is required.")]
        public DateTime StartDatetime { get; set; }

        [Required(ErrorMessage = "DurationMinutes is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1 minute.")]
        public int DurationMinutes { get; set; }

        public string? Location { get; set; }   // required if mode is physical or hybrid

        public string? ZoomJoinUrl { get; set; }    // required if mode is virtual or hybrid

        public List<int>? DepartmentIds { get; set; } // required if audience_type is departments
    }

    public class UpdateMeetingRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mode is required.")]
        public string Mode { get; set; } = string.Empty;

        [Required(ErrorMessage = "AudienceType is required.")]
        public string AudienceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "StartDatetime is required.")]
        public DateTime StartDatetime { get; set; }

        [Required(ErrorMessage = "DurationMinutes is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1 minute.")]
        public int DurationMinutes { get; set; }

        public string? Location { get; set; }   // required if mode is physical or hybrid

        public string? ZoomJoinUrl { get; set; }    // required if mode is virtual or hybrid

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = string.Empty;

        public List<int>? DepartmentIds { get; set; }
    }

    public class MeetingFilterRequest
    {
        public string? Search { get; set; }
        public string? Mode { get; set; }
        public string? AudienceType { get; set; }
        public string? Status { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? DepartmentId { get; set; }
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;
    }
}