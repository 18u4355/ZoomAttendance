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
        public async Task<ApiResponse<StaffAttendanceHistoryResponse>> GetPhysicalAttendanceHistoryAsync(string email)
        {
            try
            {
                var staff = await _context.Staff
                    .FirstOrDefaultAsync(s => s.Email.ToLower() == email.Trim().ToLower());

                if (staff == null)
                    return ApiResponse<StaffAttendanceHistoryResponse>.Fail("Staff not found.");

                var logs = await _context.AttendanceLogs
                    .Where(a => a.StaffId == staff.Id)
                    .Include(a => a.Meeting)
                    .OrderByDescending(a => a.ScannedAt)
                    .ToListAsync();

                var history = logs.Select(a => new AttendanceHistoryResponse
                {
                    MeetingName = a.Meeting.Title,
                    MeetingDate = a.Meeting.CreatedAt,
                    IsPresent = true // if log exists, they were present
                }).ToList();

                var response = new StaffAttendanceHistoryResponse
                {
                    StaffName = staff.StaffName,
                    Email = staff.Email,
                    Department = staff.Department,
                    TotalMeetings = history.Count,
                    TotalPresent = history.Count,
                    TotalAbsent = 0, // physical = only logged when present
                    History = history
                };

                return ApiResponse<StaffAttendanceHistoryResponse>.Success(response, "Attendance history retrieved.");
            }
            catch (Exception ex)
            {
                return ApiResponse<StaffAttendanceHistoryResponse>.Fail("Failed to retrieve history.", ex.Message);
            }
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

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var items = await query
                    .OrderByDescending(u => u.UserId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new staffResponse
                    {
                        UserId = u.UserId,
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
        public async Task<ApiResponse<bool>> DeleteVirtualStaffAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return ApiResponse<bool>.Fail($"Staff with ID {id} not found.");

            if (user.Role.ToLower() != "staff")
                return ApiResponse<bool>.Fail("User is not a staff member.");

            var hasAttendance = await _context.Attendance
                .AnyAsync(a => a.StaffEmail == user.Email);

            if (hasAttendance)
                return ApiResponse<bool>.Fail("Cannot delete staff with existing attendance records.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Success(true, $"{user.StaffName} has been deleted successfully.");
        }
        public async Task<ApiResponse<StaffAttendanceHistoryResponse>> GetVirtualAttendanceHistoryAsync(string email)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower()
                                           && u.Role.ToLower() == "staff");

                if (user == null)
                    return ApiResponse<StaffAttendanceHistoryResponse>.Fail("Staff not found.");

                // All meetings
                var allMeetings = await _context.Meetings.ToListAsync();

                // Meetings this staff attended
                var attended = await _context.Attendance
                    .Where(a => a.StaffEmail.ToLower() == email.Trim().ToLower()
                             && a.ConfirmAttendance == true)
                    .ToListAsync();

                var attendedMeetingIds = attended.Select(a => a.MeetingId).ToHashSet();

                var history = allMeetings
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new AttendanceHistoryResponse
                    {
                        MeetingName = m.Title,
                        MeetingDate = m.CreatedAt,
                        IsPresent = attendedMeetingIds.Contains(m.MeetingId)
                    }).ToList();

                var response = new StaffAttendanceHistoryResponse
                {
                    StaffName = user.StaffName,
                    Email = user.Email,
                    Department = user.Department ?? string.Empty,
                    TotalMeetings = history.Count,
                    TotalPresent = history.Count(h => h.IsPresent),
                    TotalAbsent = history.Count(h => !h.IsPresent),
                    History = history
                };

                return ApiResponse<StaffAttendanceHistoryResponse>.Success(response, "Attendance history retrieved.");
            }
            catch (Exception ex)
            {
                return ApiResponse<StaffAttendanceHistoryResponse>.Fail("Failed to retrieve history.", ex.Message);
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