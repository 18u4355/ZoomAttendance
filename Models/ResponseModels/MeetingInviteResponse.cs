// Models/ResponseModels/MeetingInviteResponse.cs
// StaffId changed to Guid

namespace ZoomAttendance.Models.ResponseModels
{
    public class MeetingEmailPreviewResponse
    {
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class SendInvitesResponse
    {
        public int TotalStaff { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MeetingInviteResponse
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string StaffEmail { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; }
        public DateTime? ResentAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? JoinedAt { get; set; }
        public string? AttendanceMode { get; set; }
        public string? ZoomRegistrantId { get; set; }
        public string? ZoomRegistrantJoinUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SaveLocationResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
