// Models/RequestModels/StaffRequests.cs

using System.ComponentModel.DataAnnotations;

namespace ZoomAttendance.Models.RequestModels
{
    public class CreateStaffRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "DepartmentId is required.")]
        public int DepartmentId { get; set; }
    }

    public class UpdateStaffRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "DepartmentId is required.")]
        public int DepartmentId { get; set; }
    }

    public class UpdateStaffStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = string.Empty; // active | inactive
    }

    public class StaffFilterRequest
    {
        public string? Search { get; set; }
        public int? DepartmentId { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;
    }
}