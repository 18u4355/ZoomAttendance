using Microsoft.EntityFrameworkCore;
using System.Text;
using ZoomAttendance.Data;
using ZoomAttendance.Models;
using ZoomAttendance.Models.Entities;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;
using ZoomAttendance.Services;

namespace ZoomAttendance.Repositories.Implementations
{
    public class MeetingRepository : IMeetingRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public MeetingRepository(
            ApplicationDbContext db,
            IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        public async Task<ApiResponse<bool>> CreateMeetingAsync(CreateMeetingRequest request, int hrId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                    return ApiResponse<bool>.Fail("Meeting title is required");

                if (string.IsNullOrWhiteSpace(request.ZoomUrl))
                    return ApiResponse<bool>.Fail("Zoom URL is required");

                if (!Uri.TryCreate(request.ZoomUrl, UriKind.Absolute, out var uri))
                    return ApiResponse<bool>.Fail("Invalid Zoom URL format");

                if (!uri.Host.Contains("zoom.us"))
                    return ApiResponse<bool>.Fail("URL must be a valid Zoom meeting link");

                if (!request.ZoomUrl.Contains("/j/"))
                    return ApiResponse<bool>.Fail("Invalid Zoom meeting link format");

                var meeting = new Meeting
                {
                    Title = request.Title,
                    ZoomUrl = request.ZoomUrl,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Meetings.Add(meeting);
                await _db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Meeting created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail("Failed to create meeting", ex.Message);
            }
        }
        public async Task<ApiResponse<PaginatedResponse<MeetingResponse>>> GetAllMeetingsAsync(
    int page = 1,
    int pageSize = 10,
    string status = null,
    string search = null)
        {
            var query = _db.Meetings.AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToLower() == "active")
                    query = query.Where(m => m.IsActive);
                else if (status.ToLower() == "closed")
                    query = query.Where(m => !m.IsActive);
            }

            // Filter by search
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.Title.Contains(search));

            var totalCount = await query.CountAsync();

            var meetings = await query

                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MeetingResponse
                {
                    MeetingId = m.MeetingId,
                    Title = m.Title,
                    IsActive = m.IsActive,
                    ZoomUrl = m.ZoomUrl,
                    CreatedAt = m.CreatedAt,
                    TotalInvited = _db.Attendance.Count(a => a.MeetingId == m.MeetingId),
                    TotalJoined = _db.Attendance.Count(a => a.MeetingId == m.MeetingId && a.JoinTime != null),
                    TotalConfirmed = _db.Attendance.Count(a => a.MeetingId == m.MeetingId && a.ConfirmAttendance),
                })
                .ToListAsync();

            var response = new PaginatedResponse<MeetingResponse>
            {
                Items = meetings,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return ApiResponse<PaginatedResponse<MeetingResponse>>.Success(response);
        }
        public async Task<ApiResponse<MeetingDetailResponse>> GetMeetingByIdAsync(int meetingId)
        {
            var meeting = await _db.Meetings
                .FirstOrDefaultAsync(m => m.MeetingId == meetingId);

            if (meeting == null)
                return ApiResponse<MeetingDetailResponse>.Fail("Meeting not found");

            var totalInvited = await _db.Attendance.CountAsync(a => a.MeetingId == meetingId);
            var totalJoined = await _db.Attendance.CountAsync(a => a.MeetingId == meetingId && a.JoinTime != null);
            var totalConfirmed = await _db.Attendance.CountAsync(a => a.MeetingId == meetingId && a.ConfirmAttendance);

            var response = new MeetingDetailResponse
            {
                MeetingId = meeting.MeetingId,
                Title = meeting.Title,
                IsActive = meeting.IsActive,
                ZoomUrl = meeting.ZoomUrl,
                TotalInvited = totalInvited,
                TotalJoined = totalJoined,
                TotalConfirmed = totalConfirmed

            };

            return ApiResponse<MeetingDetailResponse>.Success(response);
        }


        public async Task<ApiResponse<List<MeetingAttendanceResponse>>> GetMeetingAttendanceAsync(int meetingId)
        {
            var meetingExists = await _db.Meetings
                .AnyAsync(m => m.MeetingId == meetingId);

            if (!meetingExists)
                return ApiResponse<List<MeetingAttendanceResponse>>.Fail("Meeting not found");

            var attendanceList = await _db.Attendance
                .Where(a => a.MeetingId == meetingId)
                .Select(a => new MeetingAttendanceResponse
                {
                    AttendanceId = a.AttendanceId,
                    StaffName = a.User.StaffName,
                    StaffEmail = a.StaffEmail,
                    JoinTime = a.JoinTime,
                    ConfirmAttendance = a.ConfirmAttendance,
                    ConfirmationTime = a.ConfirmationTime,
                })
                .ToListAsync();

            return ApiResponse<List<MeetingAttendanceResponse>>
                .Success(attendanceList);
        }

        public async Task<ApiResponse<DashboardSummaryResponse>> GetDashboardSummaryAsync()
        {
            var totalMeetings = await _db.Meetings.CountAsync();
            var activeMeetings = await _db.Meetings.CountAsync(m => m.IsActive);
            var closedMeetings = await _db.Meetings.CountAsync(m => !m.IsActive);

            var totalInvited = await _db.Attendance.CountAsync();
            var totalConfirmed = await _db.Attendance.CountAsync(a => a.ConfirmAttendance);

            var response = new DashboardSummaryResponse
            {
                TotalMeetings = totalMeetings,
                ActiveMeetings = activeMeetings,
                ClosedMeetings = closedMeetings,
                TotalInvited = totalInvited,
                TotalConfirmed = totalConfirmed
            };

            return ApiResponse<DashboardSummaryResponse>.Success(response);
        }

        public async Task<byte[]?> ExportAttendanceAsync(int meetingId)
        {
            if (meetingId <= 0)
                return null;

            var meeting = await _db.Meetings
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MeetingId == meetingId);

            if (meeting == null)
                return null;

            var attendanceList = await _db.Attendance
              .AsNoTracking()
              .Where(a => a.MeetingId == meetingId)
              .Join(_db.Users,
              attendance => attendance.StaffEmail,
              user => user.Email,
              (attendance, user) => new
              {
              user.StaffName,
              attendance.StaffEmail,
              attendance.JoinTime,
              attendance.ConfirmAttendance,
              attendance.ConfirmationTime
          })
    .ToListAsync();

            if (!attendanceList.Any())
                return null;

            var csv = new StringBuilder();

            csv.AppendLine("Staff Name,Staff Emaill,Join Time,Confirmed,Confirmed Time");

            foreach (var a in attendanceList)
            {
                csv.AppendLine(
                    $"{(a.StaffName)}," +
                    $"{(a.StaffEmail)}," +
                    $"{a.JoinTime?.ToString("yyyy-MM-dd HH:mm:ss")}," +
                    $"{a.ConfirmAttendance}," +
                    $"{a.ConfirmationTime?.ToString("yyyy-MM-dd HH:mm:ss")}"
                );
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }
    }
}