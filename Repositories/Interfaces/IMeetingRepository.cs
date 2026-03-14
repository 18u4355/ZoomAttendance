// Repositories/Interfaces/IMeetingRepository.cs

using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IMeetingRepository
    {
        Task<PagedMeetingResponse> GetAllAsync(MeetingFilterRequest filter);
        Task<MeetingResponse?> GetByIdAsync(int id);
        Task<MeetingResponse> CreateAsync(CreateMeetingRequest request);
        Task<MeetingResponse> UpdateAsync(int id, UpdateMeetingRequest request);
        Task DeleteAsync(int id);
        Task<List<int>> GetMeetingsDueForInviteSendAsync();
        Task MarkInviteProcessingAsync(int meetingId);
        Task MarkInviteSentAsync(int meetingId);
        Task MarkInviteFailedAsync(int meetingId);
        Task<byte[]> ExportAsync(MeetingFilterRequest filter);
    }
}