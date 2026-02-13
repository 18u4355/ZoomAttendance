using ZoomAttendance.Models;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IMeetingRepository
    {
        Task<ApiResponse<bool>> CreateMeetingAsync(CreateMeetingRequest request, int hrId);
        Task<ApiResponse<PaginatedResponse<MeetingResponse>>> GetAllMeetingsAsync( int page = 1, int pageSize = 10, string status = null, string search = null);
        Task<ApiResponse<MeetingDetailResponse>> GetMeetingByIdAsync(int meetingId);

        Task<ApiResponse<DashboardSummaryResponse>> GetDashboardSummaryAsync();
        Task<byte[]> ExportAttendanceAsync(int meetingId);

    }
}
