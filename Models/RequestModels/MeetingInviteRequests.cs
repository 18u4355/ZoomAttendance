// Models/RequestModels/MeetingInviteRequests.cs

using System.ComponentModel.DataAnnotations;

namespace ZoomAttendance.Models.RequestModels
{
    public class SaveMeetingLocationRequest
    {
        [Required]
        public int MeetingId { get; set; }

        [Required]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal Longitude { get; set; }

        [Range(1, 100000, ErrorMessage = "Radius must be between 1 and 100000 metres.")]
        public int RadiusMetres { get; set; } = 100;
    }

    public class ConfirmPhysicalAttendanceRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal Longitude { get; set; }
    }

    public class ConfirmVirtualJoinRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}