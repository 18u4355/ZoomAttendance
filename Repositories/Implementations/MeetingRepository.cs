using Microsoft.EntityFrameworkCore;
using System.Text;
using ZoomAttendance.Data;
using ZoomAttendance.Entities;
using ZoomAttendance.Models;
using ZoomAttendance.Models.Entities;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;
using ZoomAttendance.Services;
using static System.Net.WebRequestMethods;

namespace ZoomAttendance.Repositories.Implementations
{
    public class MeetingRepository : IMeetingRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;
        private readonly IAttendanceRepository _attendanceRepository;


        public MeetingRepository(ApplicationDbContext db, IEmailService emailService, IAttendanceRepository attendanceRepository)
        {
            _db = db;
            _emailService = emailService;
            _attendanceRepository = attendanceRepository;
        }

        // ── ONLINE FLOW ───────────────────────────────────────────────────────

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
                    StartTime = request.StartTime,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = hrId
                };

                _db.Meetings.Add(meeting);
                await _db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Meeting created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail("Failed to create meeting", ex.ToString());
            }
        }

        public async Task<ApiResponse<PaginatedResponse<MeetingResponse>>> GetAllMeetingsAsync(
            int page = 1,
            int pageSize = 10,
            string status = null,
            string search = null)
        {
            var query = _db.Meetings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToLower() == "active")
                    query = query.Where(m => m.IsActive);
                else if (status.ToLower() == "closed")
                    query = query.Where(m => !m.IsActive);
            }

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

            return ApiResponse<MeetingDetailResponse>.Success(new MeetingDetailResponse
            {
                MeetingId = meeting.MeetingId,
                Title = meeting.Title,
                IsActive = meeting.IsActive,
                ZoomUrl = meeting.ZoomUrl,
                TotalInvited = totalInvited,
                TotalJoined = totalJoined,
                TotalConfirmed = totalConfirmed
            });
        }

        public async Task<ApiResponse<List<MeetingAttendanceResponse>>> GetMeetingAttendanceAsync(int meetingId)
        {
            var meetingExists = await _db.Meetings.AnyAsync(m => m.MeetingId == meetingId);

            if (!meetingExists)
                return ApiResponse<List<MeetingAttendanceResponse>>.Fail("Meeting not found");

            var attendanceList = await _db.Attendance
                .Where(a => a.MeetingId == meetingId)
                .Select(a => new MeetingAttendanceResponse
                {
                    AttendanceId = a.AttendanceId,
                    StaffName = a.StaffName,
                    StaffEmail = a.StaffEmail,
                    JoinTime = a.JoinTime,
                    ConfirmAttendance = a.ConfirmAttendance,
                    ConfirmationTime = a.ConfirmationTime,
                })
                .ToListAsync();

            return ApiResponse<List<MeetingAttendanceResponse>>.Success(attendanceList);
        }

        public async Task<ApiResponse<DashboardSummaryResponse>> GetDashboardSummaryAsync()
        {
            var totalMeetings = await _db.Meetings.CountAsync();
            var activeMeetings = await _db.Meetings.CountAsync(m => m.IsActive);
            var closedMeetings = await _db.Meetings.CountAsync(m => !m.IsActive);
            var totalInvited = await _db.Attendance.CountAsync();
            var totalConfirmed = await _db.Attendance.CountAsync(a => a.ConfirmAttendance);

            return ApiResponse<DashboardSummaryResponse>.Success(new DashboardSummaryResponse
            {
                TotalMeetings = totalMeetings,
                ActiveMeetings = activeMeetings,
                ClosedMeetings = closedMeetings,
                TotalInvited = totalInvited,
                TotalConfirmed = totalConfirmed
            });
        }

        public async Task<byte[]?> ExportAttendanceAsync(int meetingId)
        {
            if (meetingId <= 0) return null;

            var meeting = await _db.Meetings.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MeetingId == meetingId);

            if (meeting == null) return null;

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

            if (!attendanceList.Any()) return null;

            var csv = new StringBuilder();
            csv.AppendLine("Staff Name,Staff Email,Join Time,Confirmed,Confirmed Time");

            foreach (var a in attendanceList)
            {
                csv.AppendLine(
                    $"{a.StaffName}," +
                    $"{a.StaffEmail}," +
                    $"{a.JoinTime?.ToString("yyyy-MM-dd HH:mm:ss")}," +
                    $"{a.ConfirmAttendance}," +
                    $"{a.ConfirmationTime?.ToString("yyyy-MM-dd HH:mm:ss")}"
                );
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public async Task<ApiResponse<int>> SendMeetingInvitesAsync(
    int meetingId,
    SendMeetingInviteRequest request)
        {
            try
            {
                if (request.VirtualStaffEmails == null ||
                    !request.VirtualStaffEmails.Any())
                {
                    return ApiResponse<int>.Fail(
                        "At least one staff email must be provided.");
                }

                // Validate meeting
                var meeting = await _db.Meetings
                    .FirstOrDefaultAsync(m =>
                        m.MeetingId == meetingId && m.IsActive);

                if (meeting == null)
                    return ApiResponse<int>.Fail(
                        "Meeting not found or inactive");

                // Fetch valid staff emails from DB
                var validStaffEmails = await _db.Users
                    .Where(u => u.Role.ToLower() == "staff")
                    .Select(u => u.Email)
                    .ToListAsync();

                // Ensure all supplied emails exist
                if (!request.VirtualStaffEmails
                    .All(e => validStaffEmails.Contains(e)))
                {
                    return ApiResponse<int>.Fail(
                        "One or more supplied emails are not valid staff.");
                }

                int sent = 0;

                foreach (var email in request.VirtualStaffEmails)
                {
                    try
                    {
                        var result = await _attendanceRepository
                            .GenerateJoinTokenAsync(
                                meetingId,
                                email,
                                AttendanceChannel.Virtual);

                        if (!result.IsSuccessful)
                        {
                            Console.WriteLine(result.Message);
                            continue;
                        }

                        var token = result.Data;

                        var joinLink =
                           //$"https://localhost:7067/api/attendance/join?token={token}";//
                            $"http://207.180.246.69:7200/api/attendance/join?token={token}";

                        await SendZoomEmail(
                            email,
                            meeting.Title,
                            joinLink);

                        sent++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending to {email}");
                        Console.WriteLine(ex.Message);
                    }
                }

                return ApiResponse<int>.Success(
                    sent,
                    $"Virtual invites sent successfully. Total: {sent}");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Fail(
                    $"Error sending invites: {ex.Message}");
            }
        }


        private async Task SendZoomEmail(
string email,
string meetingTitle,
string joinLink)
        {
            var body = $@"
    <html>
    <body style='font-family: Arial; background:#f4f6f9; padding:30px;'>
        <div style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:8px;'>

            <h2 style='color:#2d8cff;'>Virtual Meeting Invitation</h2>

            <p>You are invited to:</p>

            <h3>{meetingTitle}</h3>

            <p>
                Click the button below to join the meeting:
            </p>

            <div style='text-align:center; margin:25px 0;'>
                <a href='{joinLink}'
                   style='background:#2d8cff;color:white;padding:12px 20px;
                          text-decoration:none;border-radius:5px;'>
                    Join Meeting
                </a>
            </div>

        </div>
    </body>
    </html>";

            await _emailService.SendEmailAsync(
                email,
                $"Meeting Invitation — {meetingTitle}",
                body
            );
        }

        // ── PHYSICAL FLOW ─────────────────────────────────────────────────────

        public async Task<ApiResponse<List<StaffEmailResponse>>> GetAllStaffEmailsAsync()
        {
            var staff = await _db.Staff
                .OrderBy(s => s.StaffName)
                .Select(s => new StaffEmailResponse
                {
                    Id = s.Id,
                    StaffName = s.StaffName,
                    Email = s.Email,
                    Department = s.Department
                })
                .ToListAsync();

            return ApiResponse<List<StaffEmailResponse>>.Success(staff);
        }

        public async Task<ApiResponse<MeetingPhysicalSummaryResponse>> GetMeetingPhysicalSummaryAsync(int meetingId)
        {
            var meeting = await _db.Meetings
                .FirstOrDefaultAsync(m => m.MeetingId == meetingId);

            if (meeting == null)
                return ApiResponse<MeetingPhysicalSummaryResponse>.Fail("Meeting not found");

            var totalPhysical = await _db.AttendanceLogs
                .CountAsync(a => a.MeetingId == meetingId);

            return ApiResponse<MeetingPhysicalSummaryResponse>.Success(new MeetingPhysicalSummaryResponse
            {
                MeetingId = meeting.MeetingId,
                MeetingTitle = meeting.Title,
                MeetingDate = meeting.CreatedAt,
                TotalPhysicalAttendees = totalPhysical
            });
        }

        public async Task<byte[]?> ExportPhysicalAttendanceAsync(int meetingId)
        {
            if (meetingId <= 0) return null;

            var meeting = await _db.Meetings.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MeetingId == meetingId);

            if (meeting == null) return null;

            var logs = await _db.AttendanceLogs
                .AsNoTracking()
                .Include(a => a.Staff)
                .Where(a => a.MeetingId == meetingId)
                .OrderBy(a => a.ScannedAt)
                .ToListAsync();

            if (!logs.Any()) return null;

            var csv = new StringBuilder();
            csv.AppendLine("Staff Name,Department,Email,Scanned At");

            foreach (var log in logs)
            {
                csv.AppendLine(
                    $"{log.Staff.StaffName}," +
                    $"{log.Staff.Department}," +
                    $"{log.Staff.Email}," +
                    $"{log.ScannedAt:yyyy-MM-dd HH:mm:ss}"
                );
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }
    }
}