// Repositories/Interfaces/IDepartmentRepository.cs

using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<IEnumerable<DepartmentResponse>> GetAllAsync();
        Task<DepartmentResponse?> GetByIdAsync(int id);
        Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request);
        Task<DepartmentResponse> UpdateAsync(int id, UpdateDepartmentRequest request);
        Task DeleteAsync(int id);
        Task<byte[]> ExportAsync();
    }
}