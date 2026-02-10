using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IMeetingRepository
    {
        Task<ApiResponse<bool>> CreateMeetingAsync(CreateMeetingRequest request, int hrId);
        Task<ApiResponse<List<MeetingResponse>>> GetActiveMeetingsAsync();
      
    }
}
