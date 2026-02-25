using Microsoft.EntityFrameworkCore;
using ZoomAttendance.Data;
using ZoomAttendance.Entities;
using ZoomAttendance.Models;
using ZoomAttendance.Models.Entities;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Models.ResponseModels.ZoomAttendance.Models.ResponseModels;
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

        // ── Physical staff (QR attendance) ────────────────────────────────────

        public async Task<ApiResponse<StaffResponseQr>> RegisterAsync(RegisterStaffRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StaffName))
                return ApiResponse<StaffResponseQr>.Fail("Full name is required.");

            if (request.StaffName.Trim().Length < 2)
                return ApiResponse<StaffResponseQr>.Fail("Full name must be at least 2 characters.");

            if (request.StaffName.Trim().Length > 100)
                return ApiResponse<StaffResponseQr>.Fail("Full name must not exceed 100 characters.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return ApiResponse<StaffResponseQr>.Fail("Email is required.");

            if (!IsValidEmail(request.Email))
                return ApiResponse<StaffResponseQr>.Fail("Invalid email format.");

            if (request.Email.Length > 150)
                return ApiResponse<StaffResponseQr>.Fail("Email must not exceed 150 characters.");

            if (string.IsNullOrWhiteSpace(request.Department))
                return ApiResponse<StaffResponseQr>.Fail("Department is required.");

            if (request.Department.Trim().Length < 2)
                return ApiResponse<StaffResponseQr>.Fail("Department must be at least 2 characters.");

            if (request.Department.Trim().Length > 100)
                return ApiResponse<StaffResponseQr>.Fail("Department must not exceed 100 characters.");

            var emailExists = await _context.Staff
                .AnyAsync(s => s.Email.ToLower() == request.Email.Trim().ToLower());

            if (emailExists)
                return ApiResponse<StaffResponseQr>.Fail("A staff member with this email already exists.");

            var staff = new Staff
            {
                StaffName = request.StaffName.Trim(),
                Email = request.Email.Trim().ToLower(),
                Department = request.Department.Trim(),
                BarcodeToken = Guid.NewGuid().ToString("N")
            };

            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            return ApiResponse<StaffResponseQr>.Success(MapToQrResponse(staff), "Staff registered successfully.");
        }

        public async Task<ApiResponse<PaginatedResponse<StaffResponseQr>>> GetAllAsync(PaginatedStaffRequest request)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize > 100 ? 100 : request.PageSize;
            var search = request.Search?.Trim().ToLower();

            var query = _context.Staff.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.StaffName.ToLower().Contains(search) ||
                    s.Email.ToLower().Contains(search) ||
                    s.Department.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var items = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PaginatedResponse<StaffResponseQr>
            {
                Items = items.Select(MapToQrResponse),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return ApiResponse<PaginatedResponse<StaffResponseQr>>.Success(result);
        }

        public async Task<ApiResponse<StaffResponseQr>> GetByIdAsync(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null)
                return ApiResponse<StaffResponseQr>.Fail($"Staff with ID {id} not found.");

            return ApiResponse<StaffResponseQr>.Success(MapToQrResponse(staff));
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null)
                return ApiResponse<bool>.Fail($"Staff with ID {id} not found.");

            var hasAttendance = await _context.AttendanceLogs
                .AnyAsync(a => a.StaffId == id);

            if (hasAttendance)
                return ApiResponse<bool>.Fail("Cannot delete staff with existing attendance records.");

            _context.Staff.Remove(staff);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Success(true, $"{staff.StaffName} has been deleted successfully.");
        }

        // ── Virtual staff (Zoom attendance) ───────────────────────────────────

        public async Task<ApiResponse<bool>> CreateStaffAsync(CreateStaffRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StaffName))
                return ApiResponse<bool>.Fail("Full name is required.");

            if (request.StaffName.Trim().Length < 2)
                return ApiResponse<bool>.Fail("Full name must be at least 2 characters.");

            if (request.StaffName.Trim().Length > 100)
                return ApiResponse<bool>.Fail("Full name must not exceed 100 characters.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return ApiResponse<bool>.Fail("Email is required.");

            if (!IsValidEmail(request.Email))
                return ApiResponse<bool>.Fail("Invalid email format.");

            if (request.Email.Length > 150)
                return ApiResponse<bool>.Fail("Email must not exceed 150 characters.");

            if (string.IsNullOrWhiteSpace(request.Department))
                return ApiResponse<bool>.Fail("Department is required.");

            if (request.Department.Trim().Length < 2)
                return ApiResponse<bool>.Fail("Department must be at least 2 characters.");

            if (request.Department.Trim().Length > 100)
                return ApiResponse<bool>.Fail("Department must not exceed 100 characters.");

            var existing = await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower() == request.Email.Trim().ToLower());

            if (existing != null)
                return ApiResponse<bool>.Fail("A staff member with this email already exists.");

            var user = new User
            {
                Email = request.Email.Trim().ToLower(),
                StaffName = request.StaffName.Trim(),
                Department = request.Department.Trim(),
                Role = "staff",
                PasswordHash = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Success(true, "Staff created successfully.");
        }

        public async Task<ApiResponse<PaginatedResponse<staffResponse>>> GetAllStaffAsync(PaginatedStaffRequest request)
        {
            try
            {
                var page = request.Page < 1 ? 1 : request.Page;
                var pageSize = request.PageSize < 1 ? 10 : request.PageSize > 100 ? 100 : request.PageSize;
                var search = request.Search?.Trim().ToLower();

                var query = _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Where(u => u.Role.ToLower() == "staff")
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(u =>
                        u.StaffName.ToLower().Contains(search) ||
                        u.Email.ToLower().Contains(search) ||
                        u.Department.ToLower().Contains(search));
                }

                var totalCount = await query.CountAsync();

                if (totalCount == 0)
                    return ApiResponse<PaginatedResponse<staffResponse>>.Fail("No staff found.");

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var items = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new staffResponse
                    {
                        StaffName = u.StaffName,
                        Email = u.Email,
                        Department = u.Department
                    })
                    .ToListAsync();

                var result = new PaginatedResponse<staffResponse>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages
                };

                return ApiResponse<PaginatedResponse<staffResponse>>.Success(result, "Staff retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<staffResponse>>.Fail("Failed to retrieve staff.", ex.Message);
            }
        }
        // ── Private helpers ───────────────────────────────────────────────────

        private static StaffResponseQr MapToQrResponse(Staff s) => new()
        {
            Id = s.Id,
            StaffName = s.StaffName,
            Email = s.Email,
            Department = s.Department,
            BarcodeToken = s.BarcodeToken,
            CreatedAt = s.CreatedAt
        };

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }
    }
}