// Repositories/Interfaces/IAttendanceRepository.cs
// Checkout methods removed

using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IAttendanceRepository
    {
        Task InitializeAsync(int meetingId, int staffId, string mode);
        Task<CheckInResponse> PhysicalCheckInAsync(string token, decimal latitude, decimal longitude);
        Task<VirtualJoinResponse> VirtualJoinAsync(string token);
        Task<CheckInResponse> VirtualEndConfirmAsync(string token);
        Task<PagedAttendanceResponse> GetAttendanceAsync(int meetingId, AttendanceFilterRequest filter);
        Task<AttendanceSummaryResponse> GetSummaryAsync(int meetingId);
        Task UpdateStatusAsync(int meetingId, int staffId, string status);
        Task<byte[]> ExportAsync(int meetingId, AttendanceFilterRequest filter);
        Task<List<PendingVirtualConfirm>> GetPendingVirtualConfirmsAsync();
        Task SaveEndConfirmTokenAsync(int meetingId, int staffId, string token);
    }
}