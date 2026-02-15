using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

public interface ISettingsRepository
{
    Task<ApiResponse<AccountProfileResponse>> GetMySettingsAsync(int userId);
    Task<ApiResponse<string>> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}