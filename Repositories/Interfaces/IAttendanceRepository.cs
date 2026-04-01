// Repositories/Interfaces/IAttendanceRepository.cs
// StaffId changed to Guid

using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IAttendanceRepository
    {
        Task InitializeAsync(int meetingId, Guid staffId, string mode);
        Task<CheckInResponse> PhysicalCheckInAsync(string token, decimal latitude, decimal longitude);
        Task<VirtualJoinResponse> VirtualJoinAsync(string token);
        Task<CheckInResponse> VirtualEndConfirmAsync(string token);
        Task<AttendanceSummaryResponse> GetSummaryAsync(int meetingId);
        Task<byte[]> ExportAsync(int meetingId, AttendanceFilterRequest filter);
        Task<List<PendingVirtualConfirm>> GetPendingVirtualConfirmsAsync();
        Task SaveEndConfirmTokenAsync(int meetingId, Guid staffId, string token);
        Task<PagedAttendanceResponse> GetAttendanceAsync(AttendanceFilterRequest filter);
        Task<StaffAttendanceReportResponse> GetStaffReportAsync(Guid staffId, StaffAttendanceReportRequest request);
    }
}