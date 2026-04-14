// Repositories/Interfaces/IDepartmentRepository.cs

using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<IEnumerable<DepartmentResponse>> GetAllAsync(string? status, int pageNumber,int pageSize);
        Task<DepartmentResponse?> GetByIdAsync(int id);
        Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request);
        Task<DepartmentResponse> UpdateAsync(int id, UpdateDepartmentRequest request);
        Task DeactivateAsync(int id);
        Task<DepartmentResponse> ActivateAsync(int id);
        Task<byte[]> ExportAsync(bool includeInactive = false);
        Task<DepartmentMeetingSummaryResponse?> GetMeetingSummaryAsync(int deptId);
    }
}