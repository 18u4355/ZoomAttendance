using ZoomAttendance.Models;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IMeetingRepository
    {
        // ── ONLINE FLOW ───────────────────────────────────────────────────────
        Task<ApiResponse<bool>> CreateMeetingAsync(CreateMeetingRequest request, int hrId);
        Task<ApiResponse<PaginatedResponse<MeetingResponse>>> GetAllMeetingsAsync(int page, int pageSize, string status, string search);
        Task<ApiResponse<MeetingDetailResponse>> GetMeetingByIdAsync(int meetingId);
        Task<ApiResponse<List<MeetingAttendanceResponse>>> GetMeetingAttendanceAsync(int meetingId);
        Task<ApiResponse<DashboardSummaryResponse>> GetDashboardSummaryAsync();
        Task<byte[]?> ExportAttendanceAsync(int meetingId);

        // ── PHYSICAL FLOW ─────────────────────────────────────────────────────
        Task<ApiResponse<List<StaffEmailResponse>>> GetAllStaffEmailsAsync();
        Task<ApiResponse<MeetingPhysicalSummaryResponse>> GetMeetingPhysicalSummaryAsync(int meetingId);
        Task<byte[]?> ExportPhysicalAttendanceAsync(int meetingId);
        Task<ApiResponse<int>> SendMeetingInvitesAsync(
           int meetingId,
           SendMeetingInviteRequest request);
    }
}