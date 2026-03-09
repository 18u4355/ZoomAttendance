// Models/RequestModels/DepartmentRequests.cs

using System.ComponentModel.DataAnnotations;

namespace ZoomAttendance.Models.RequestModels
{
    public class CreateDepartmentRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateDepartmentRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}