using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IHrRepository
    {
        Task<ApiResponse<string>> InviteHrAsync(InviteHrRequest request);
        Task<ApiResponse<string>> CompleteHrSetupAsync(CompleteHrSetupRequest request);
    }
}