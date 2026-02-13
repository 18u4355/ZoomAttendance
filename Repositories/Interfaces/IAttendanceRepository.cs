using ZoomAttendance.Models;
using ZoomAttendance.Models.Entities;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<ApiResponse<string>> GenerateJoinTokenAsync(int meetingId, string staffEmail);
        Task<ApiResponse<string>> ValidateAndConfirmAsync(string token);
      //ask<ApiResponse<bool>> ConfirmAttendanceAsync(string token);
        Task<ApiResponse<bool>> CloseMeetingAsync(int meetingId);
        Task<(bool Success, string RedirectUrl)> ConfirmCloseMeetingAsync(string token);
        // Task<ApiResponse<PaginatedResponse<AttendanceReportResponse>>> GetAttendanceReportAsync(AttendanceReportRequest request);

    }
}
