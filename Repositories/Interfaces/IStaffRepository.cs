using ZoomAttendance.Models;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IStaffRepository
    {
        Task<ApiResponse<StaffResponseQr>> RegisterAsync(RegisterStaffRequest request);
        Task<ApiResponse<List<StaffResponseQr>>> GetAllAsync();
        Task<ApiResponse<StaffResponseQr>> GetByIdAsync(int id);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}