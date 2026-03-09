// Repositories/Interfaces/IStaffRepository.cs

using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IStaffRepository
    {
        Task<PagedStaffResponse> GetAllAsync(StaffFilterRequest filter);
        Task<StaffResponse?> GetByIdAsync(int id);
        Task<StaffResponse> CreateAsync(CreateStaffRequest request);
        Task<StaffResponse> UpdateAsync(int id, UpdateStaffRequest request);
        Task UpdateStatusAsync(int id, UpdateStaffStatusRequest request);
        Task DeleteAsync(int id);
        Task<byte[]> ExportAsync(StaffFilterRequest filter);
        Task<BulkUploadResponse> BulkUploadAsync(IFormFile file);
        Task<byte[]> GetUploadTemplateAsync();
    }
}