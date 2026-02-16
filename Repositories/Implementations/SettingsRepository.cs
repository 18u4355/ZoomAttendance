using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using ZoomAttendance.Data;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;

public class SettingsRepository : ISettingsRepository
{
    private readonly ApplicationDbContext _db;

    public SettingsRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<AccountProfileResponse>> GetMySettingsAsync(int userId)
    {
        try
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return ApiResponse<AccountProfileResponse>.Fail("User not found");

            var response = new AccountProfileResponse
            {
                UserId = user.UserId,
                FullName = user.StaffName,
                Email = user.Email,
                Role = user.Role
            };

            return ApiResponse<AccountProfileResponse>.Success(response, "Account retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<AccountProfileResponse>.Fail("Failed to retrieve account settings", ex.Message);
        }
    }

    public async Task<ApiResponse<string>> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return ApiResponse<string>.Fail("User not found");

            bool updated = false;

            
            if (!string.IsNullOrWhiteSpace(request.StaffName))
            {
                user.StaffName = request.StaffName.Trim();
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var email = request.Email.Trim();

                if (email.ToLower() != "string")
                {
                    if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        return ApiResponse<string>.Fail("Invalid email format");

                    var emailExists = await _db.Users.AnyAsync(u => u.Email == email && u.UserId != userId);
                    if (emailExists)
                        return ApiResponse<string>.Fail("Email already in use");

                    user.Email = email;
                    updated = true;
                }
            }

            if (!updated)
                return ApiResponse<string>.Fail("No valid fields provided to update");

            await _db.SaveChangesAsync();
            return ApiResponse<string>.Success("Profile updated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail("Failed to update profile", ex.Message);
        }
    }

    public async Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return ApiResponse<string>.Fail("Current password is required");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return ApiResponse<string>.Fail("New password is required");

            var newPassword = request.NewPassword.Trim();

            if (newPassword.Length < 6)
                return ApiResponse<string>.Fail("New password must be at least 6 characters");

            if (newPassword.ToLower() == "string")
                return ApiResponse<string>.Fail("New password cannot be a placeholder");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return ApiResponse<string>.Fail("User not found");

            bool isValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
            if (!isValid)
                return ApiResponse<string>.Fail("Current password is incorrect");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _db.SaveChangesAsync();

            return ApiResponse<string>.Success("Password changed successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail("Failed to change password", ex.Message);
        }
    }

}