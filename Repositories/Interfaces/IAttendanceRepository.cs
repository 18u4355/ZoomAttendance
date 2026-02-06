using ZoomAttendance.Models.Entities;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<ApiResponse<string>> GenerateJoinTokenAsync(int meetingId, string staffEmail);
        Task<ApiResponse<bool>> LogAttendanceAsync(string token);
        Task<ApiResponse<bool>> ConfirmAttendanceAsync(string token);
        Task<ApiResponse<List<AttendanceReportResponse>>> GetAttendanceReportAsync(DateTime start, DateTime end);
       // Task<MeetingAttendance?> GetByTokenAsync(string token);
    }
}
