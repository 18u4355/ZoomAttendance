using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    }
}
