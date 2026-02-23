using ZoomAttendance.Models;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IAttendanceRepository
    {
        // ── ONLINE FLOW ───────────────────────────────────────────────────────
        Task<ApiResponse<string>> GenerateJoinTokenAsync(
                   int meetingId,
                   string staffEmail,
                   AttendanceChannel channel); 
        Task<ApiResponse<string>> ValidateAndConfirmAsync(string token);
        Task<ApiResponse<bool>> CloseMeetingAsync(int meetingId);
        Task<(bool Success, string RedirectUrl)> ConfirmCloseMeetingAsync(string token);

        // ── PHYSICAL FLOW ─────────────────────────────────────────────────────
        Task<ApiResponse<ScanResponse>> ScanAsync(ScanAttendanceRequest request);
        Task<ApiResponse<MeetingAttendanceResponsePhysical>> GetMeetingAttendanceAsync(int meetingId);
        Task<ApiResponse<SendQrCodeResponse>> SendQrCodesAsync(SendQrCodeRequest request);
        Task<ApiResponse<List<string>>> GetAllStaffEmailsAsync();

        Task<ApiResponse<string>> GenerateAndSendLinkAsync(int meetingId, string email);
        
    }

}