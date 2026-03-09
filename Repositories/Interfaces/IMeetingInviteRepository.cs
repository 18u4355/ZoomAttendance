// Repositories/Interfaces/IMeetingInviteRepository.cs

using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IMeetingInviteRepository
    {
        Task<List<MeetingEmailPreviewResponse>> GetEmailsPreviewAsync(int meetingId);
        Task<SendInvitesResponse> SendInvitesAsync(int meetingId);
        Task ResendInviteAsync(int meetingId, int staffId);
        Task<List<MeetingInviteResponse>> GetInvitesByMeetingAsync(int meetingId);
        Task SaveLocationAsync(SaveMeetingLocationRequest request);
    }
}