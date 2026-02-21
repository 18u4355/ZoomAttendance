using Microsoft.EntityFrameworkCore;
using ZoomAttendance.Data;
using ZoomAttendance.Entities;
using ZoomAttendance.Models;
using ZoomAttendance.Models.Entities;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;
using ZoomAttendance.Services;

namespace ZoomAttendance.Repositories.Implementations
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public AttendanceRepository(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        // ── ONLINE FLOW ───────────────────────────────────────────────────────

        public async Task<ApiResponse<string>> GenerateJoinTokenAsync(int meetingId, string staffEmail)
        {
            try
            {
                var meeting = await _db.Meetings
                    .FirstOrDefaultAsync(m => m.MeetingId == meetingId && m.IsActive);

                if (meeting == null)
                    return ApiResponse<string>.Fail("Meeting not found or inactive");

                var joinToken = Guid.NewGuid().ToString();

                var attendance = new MeetingAttendance
                {
                    MeetingId = meetingId,
                    StaffEmail = staffEmail,
                    JoinToken = joinToken,
                    JoinTime = DateTime.UtcNow,
                };

                _db.Attendance.Add(attendance);
                await _db.SaveChangesAsync();

                var joinLink = $"http://207.180.246.69:7200/api/attendance/join?token={joinToken}";

                await _emailService.SendEmailAsync(
                    staffEmail,
                    "Meeting Invitation",
                    $"You are invited to '{meeting.Title}'.<br/><br/>" +
                    $"Click below to join:<br/>" +
                    $"<a href='{joinLink}'>Join Meeting</a>"
                );

                return ApiResponse<string>.Success(joinToken, "Join link generated and email sent");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail("Failed to generate join link", ex.Message);
            }
        }

        public async Task<ApiResponse<string>> ValidateAndConfirmAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ApiResponse<string>.Fail("Token is required");

            var attendance = await _db.Attendance
                .Where(a => a.JoinToken == token)
                .Select(a => new
                {
                    a.AttendanceId,
                    a.ConfirmAttendance,
                    a.ConfirmationExpiresAt,
                    MeetingIsActive = a.Meeting.IsActive,
                    MeetingClosedAt = a.Meeting.ClosedAt,
                    ZoomUrl = a.Meeting.ZoomUrl
                })
                .FirstOrDefaultAsync();

            if (attendance == null)
                return ApiResponse<string>.Fail("Invalid token");

            if (attendance.ConfirmAttendance)
                return ApiResponse<string>.Fail("Attendance already confirmed");

            if (!attendance.MeetingIsActive || attendance.MeetingClosedAt != null)
                return ApiResponse<string>.Fail("Meeting has been closed");

            if (attendance.ConfirmationExpiresAt.HasValue &&
                DateTime.UtcNow > attendance.ConfirmationExpiresAt.Value)
                return ApiResponse<string>.Fail("Token expired");

            return ApiResponse<string>.Success(attendance.ZoomUrl ?? "", "Attendance confirmed");
        }

        public async Task<ApiResponse<bool>> CloseMeetingAsync(int meetingId)
        {
            var meeting = await _db.Meetings
                .FirstOrDefaultAsync(m => m.MeetingId == meetingId);

            if (meeting == null)
                return ApiResponse<bool>.Fail("Meeting not found");

            if (!meeting.IsActive)
                return ApiResponse<bool>.Fail("Meeting already closed");

            meeting.IsActive = false;
            meeting.ClosedAt = DateTime.UtcNow;

            var attendees = await _db.Attendance
                .Where(a => a.MeetingId == meetingId && a.JoinTime != null)
                .ToListAsync();

            foreach (var attendee in attendees)
            {
                if (string.IsNullOrWhiteSpace(attendee.ConfirmationToken))
                    attendee.ConfirmationToken = Guid.NewGuid().ToString();

                attendee.ConfirmationExpiresAt = DateTime.UtcNow.AddMinutes(15);
            }

            await _db.SaveChangesAsync();

            foreach (var attendee in attendees)
            {
                var confirmLink = $"http://207.180.246.69:7200/api/attendance/confirm?token={attendee.ConfirmationToken}";

                await _emailService.SendEmailAsync(
                    attendee.StaffEmail,
                    "Confirm Your Attendance",
                    $"Please confirm your attendance for '{meeting.Title}'.<br/>" +
                    $"This link expires in 15 minutes:<br/>" +
                    $"<a href='{confirmLink}'>Confirm Attendance</a>"
                );
            }

            return ApiResponse<bool>.Success(true, "Meeting closed and confirmation emails sent");
        }

        public async Task<(bool Success, string RedirectUrl)> ConfirmCloseMeetingAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, "http://localhost:3000/confirmation/invalid");

            var attendance = await _db.Attendance
                .Include(a => a.Meeting)
                .FirstOrDefaultAsync(a => a.ConfirmationToken == token);

            if (attendance == null)
                return (false, "http://localhost:3000/confirmation/invalid");

            if (attendance.Meeting.IsActive)
                return (false, "http://localhost:3000/confirmation/invalid");

            if (attendance.ConfirmAttendance)
                return (false, "http://localhost:3000/confirmation/already");

            if (attendance.ConfirmationExpiresAt == null || attendance.ConfirmationExpiresAt < DateTime.UtcNow)
                return (false, "http://localhost:3000/confirmation/expired");

            attendance.ConfirmAttendance = true;
            attendance.ConfirmationTime = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, "http://localhost:3000/confirmation/success");
        }

        // ── PHYSICAL FLOW ─────────────────────────────────────────────────────

        public async Task<ApiResponse<ScanResponse>> ScanAsync(ScanAttendanceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return ApiResponse<ScanResponse>.Fail("Barcode token is required.");

            var staff = await _db.Staff
                .FirstOrDefaultAsync(s => s.BarcodeToken == request.Token);

            if (staff == null)
                return ApiResponse<ScanResponse>.Fail("Invalid barcode. Staff not found.");

            var meeting = await _db.Meetings
                .FirstOrDefaultAsync(m => m.MeetingId == request.MeetingId);

            if (meeting == null)
                return ApiResponse<ScanResponse>.Fail($"Meeting with ID {request.MeetingId} not found.");

            var alreadyScanned = await _db.AttendanceLogs
                .AnyAsync(a => a.StaffId == staff.Id && a.MeetingId == request.MeetingId);

            if (alreadyScanned)
                return ApiResponse<ScanResponse>.Fail("Attendance already recorded for this meeting.");

            var log = new AttendanceLog
            {
                StaffId = staff.Id,
                MeetingId = request.MeetingId,
                ScannedAt = DateTime.UtcNow
            };

            _db.AttendanceLogs.Add(log);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return ApiResponse<ScanResponse>.Fail("Attendance already recorded for this meeting.");
            }

            return ApiResponse<ScanResponse>.Success(new ScanResponse
            {
                Success = true,
                Message = "Attendance recorded successfully.",
                StaffName = staff.FullName,
                ScannedAt = log.ScannedAt
            }, "Attendance recorded successfully.");
        }

        public async Task<ApiResponse<MeetingAttendanceResponsePhysical>> GetMeetingAttendanceAsync(int meetingId)
        {
            var meeting = await _db.Meetings
                .FirstOrDefaultAsync(m => m.MeetingId == meetingId);

            if (meeting == null)
                return ApiResponse<MeetingAttendanceResponsePhysical>.Fail("Meeting not found.");

            var logs = await _db.AttendanceLogs
                .Include(a => a.Staff)
                .Where(a => a.MeetingId == meetingId)
                .OrderBy(a => a.ScannedAt)
                .ToListAsync();

            return ApiResponse<MeetingAttendanceResponsePhysical>.Success(new MeetingAttendanceResponsePhysical
            {
                MeetingId = meeting.MeetingId,
                MeetingTitle = meeting.Title,
                MeetingDate = meeting.CreatedAt,
                TotalPhysicalAttendees = logs.Count,
                Attendees = logs.Select(a => new AttendanceRecordResponse
                {
                    StaffId = a.StaffId,
                    StaffName = a.Staff.FullName,
                    Department = a.Staff.Department,
                    ScannedAt = a.ScannedAt
                }).ToList()
            });
        }

        public async Task<ApiResponse<SendQrCodeResponse>> SendQrCodesAsync(SendQrCodeRequest request)
        {
            if (request.StaffEmails == null || request.StaffEmails.Count == 0)
                return ApiResponse<SendQrCodeResponse>.Fail("At least one staff email must be selected.");

            var meeting = await _db.Meetings
                .FirstOrDefaultAsync(m => m.MeetingId == request.MeetingId);

            if (meeting == null)
                return ApiResponse<SendQrCodeResponse>.Fail($"Meeting with ID {request.MeetingId} not found.");

            var selectedStaff = await _db.Staff
                .Where(s => request.StaffEmails.Contains(s.Email))
                .OrderBy(s => s.FullName)
                .ToListAsync();

            if (selectedStaff.Count == 0)
                return ApiResponse<SendQrCodeResponse>.Fail("None of the selected staff emails were found.");

            var results = new List<QrCodeEmailResult>();

            foreach (var staff in selectedStaff)
            {
                var result = new QrCodeEmailResult
                {
                    StaffId = staff.Id,
                    StaffName = staff.FullName,
                    Email = staff.Email
                };

                try
                {
                    await _emailService.SendQrCodeEmailAsync(staff, meeting);
                    result.Sent = true;
                }
                catch (Exception ex)
                {
                    result.Sent = false;
                    result.FailureReason = ex.Message;
                }

                results.Add(result);
            }

            return ApiResponse<SendQrCodeResponse>.Success(new SendQrCodeResponse
            {
                TotalSelected = request.StaffEmails.Count,
                TotalSent = results.Count(r => r.Sent),
                TotalFailed = results.Count(r => !r.Sent),
                Results = results
            }, "QR codes processed.");
        }
    }
}