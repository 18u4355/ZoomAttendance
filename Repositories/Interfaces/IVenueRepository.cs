using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IVenueRepository
    {
        Task<IEnumerable<VenueResponse>> GetAllAsync(bool includeInactive = false);
        Task<VenueResponse?> GetByIdAsync(int id);
        Task<VenueResponse> CreateAsync(CreateVenueRequest request);
        Task<VenueResponse> UpdateAsync(int id, UpdateVenueRequest request);
        Task DeleteAsync(int id);
    }
}
