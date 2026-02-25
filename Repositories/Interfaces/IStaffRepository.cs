using ZoomAttendance.Models;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Models.ResponseModels.ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IStaffRepository
    {
        // ── Physical staff (QR attendance) ────────────────────────────────────
        Task<ApiResponse<StaffResponseQr>> RegisterAsync(RegisterStaffRequest request);
        Task<ApiResponse<PaginatedResponse<StaffResponseQr>>> GetAllAsync(PaginatedStaffRequest request);
        Task<ApiResponse<StaffResponseQr>> GetByIdAsync(int id);
        Task<ApiResponse<bool>> DeleteAsync(int id);

        // ── Virtual staff (Zoom attendance) ───────────────────────────────────
        Task<ApiResponse<bool>> CreateStaffAsync(CreateStaffRequest request);
        Task<ApiResponse<PaginatedResponse<staffResponse>>> GetAllStaffAsync(PaginatedStaffRequest request);

    }
}