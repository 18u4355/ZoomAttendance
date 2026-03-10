// Repositories/Interfaces/IMeetingInviteRepository.cs
// StaffId changed to Guid

using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IMeetingInviteRepository
    {
        Task<List<MeetingEmailPreviewResponse>> GetEmailsPreviewAsync(int meetingId);
        Task<SendInvitesResponse> SendInvitesAsync(int meetingId);
        Task ResendInviteAsync(int meetingId, Guid staffId);
        Task<List<MeetingInviteResponse>> GetInvitesByMeetingAsync(int meetingId);
    }
}