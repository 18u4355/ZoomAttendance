using Microsoft.EntityFrameworkCore;
using ZoomAttendance.Data;
using ZoomAttendance.Entities;
using ZoomAttendance.Models;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Repositories.Implementations
{
    public class StaffRepository : IStaffRepository
    {
        private readonly ApplicationDbContext _context;

        public StaffRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<StaffResponseQr>> RegisterAsync(RegisterStaffRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
                return ApiResponse<StaffResponseQr>.Fail("FullName and Email are required.");

            var emailExists = await _context.Staff.AnyAsync(s => s.Email == request.Email);
            if (emailExists)
                return ApiResponse<StaffResponseQr>.Fail("A staff member with this email already exists.");

            var staff = new Staff
            {
                FullName = request.FullName,
                Email = request.Email,
                Department = request.Department,
                BarcodeToken = Guid.NewGuid().ToString("N")
            };

            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            return ApiResponse<StaffResponseQr>.Success(MapToResponse(staff), "Staff registered successfully.");
        }

        public async Task<ApiResponse<List<StaffResponseQr>>> GetAllAsync()
        {
            var staff = await _context.Staff
                .OrderBy(s => s.FullName)
                .ToListAsync();

            return ApiResponse<List<StaffResponseQr>>.Success(staff.Select(MapToResponse).ToList());
        }

        public async Task<ApiResponse<StaffResponseQr>> GetByIdAsync(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null)
                return ApiResponse<StaffResponseQr>.Fail($"Staff with ID {id} not found.");

            return ApiResponse<StaffResponseQr>.Success(MapToResponse(staff));
        }

        private static StaffResponseQr MapToResponse(Staff s) => new()
        {
            Id = s.Id,
            FullName = s.FullName,
            Email = s.Email,
            Department = s.Department,
            BarcodeToken = s.BarcodeToken,
            CreatedAt = s.CreatedAt
        };
    }
}