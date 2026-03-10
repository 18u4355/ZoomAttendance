// Repositories/Interfaces/IStaffRepository.cs
// Id changed to Guid

using Microsoft.AspNetCore.Http;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IStaffRepository
    {
        Task<PagedStaffResponse> GetAllAsync(StaffFilterRequest filter);
        Task<StaffResponse?> GetByIdAsync(Guid id);
        Task<StaffResponse> CreateAsync(CreateStaffRequest request);
        Task<StaffResponse> UpdateAsync(Guid id, UpdateStaffRequest request);
        Task DeleteAsync(Guid id);
        Task UpdateStatusAsync(Guid id, string status);
        Task<BulkUploadResponse> BulkUploadAsync(IFormFile file);
        Task<byte[]> ExportAsync(StaffFilterRequest filter);
        Task<byte[]> GetUploadTemplateAsync();
    }
}