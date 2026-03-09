// Repositories/Interfaces/IDashboardRepository.cs

using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardResponse> GetStatsAsync();
    }
}