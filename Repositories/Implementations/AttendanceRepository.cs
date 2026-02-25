using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
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

        public async Task<ApiResponse<string>> GenerateJoinTokenAsync(int meetingId, string staffEmail, AttendanceChannel channel)
        {
            try
            {
                var meeting = await _db.Meetings
                    .FirstOrDefaultAsync(m => m.MeetingId == meetingId && m.IsActive);

                if (meeting == null)
                    return ApiResponse<string>.Fail("Meeting not found or inactive");

                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == staffEmail.ToLower());

                if (user == null)
                    return ApiResponse<string>.Fail("Staff not registered");

                var existing = await _db.Attendance
                    .FirstOrDefaultAsync(a =>
                        a.MeetingId == meetingId &&
                        a.StaffEmail == staffEmail && 
                a.AttendedVia == channel );

                if (existing != null)
                    return ApiResponse<string>.Fail("Join token already exists for this staff");

                var joinToken = Guid.NewGuid().ToString();

                var attendance = new MeetingAttendance
                {
                    MeetingId = meetingId,
                    StaffEmail = staffEmail,
                    StaffName = user.StaffName,
                    JoinToken = joinToken,
                    AttendedVia = channel

                };

                _db.Attendance.Add(attendance);
                await _db.SaveChangesAsync();

                // ❌ EMAIL REMOVED FROM HERE

                return ApiResponse<string>.Success(joinToken, "Join token generated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== GENERATE JOIN TOKEN ERROR ===");
                Console.WriteLine(ex.ToString());

                return ApiResponse<string>.Fail(
                    "Failed to generate join token",
                    ex.Message
                );
            }
        }

        public async Task<ApiResponse<string>> ValidateAndConfirmAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ApiResponse<string>.Fail("Token is required");

            var attendance = await _db.Attendance
                .Include(a => a.Meeting)
                .FirstOrDefaultAsync(a => a.JoinToken == token);

            if (attendance == null)
                return ApiResponse<string>.Fail("Invalid token");

            if (!attendance.Meeting.IsActive || attendance.Meeting.ClosedAt != null)
                return ApiResponse<string>.Fail("Meeting has been closed");

            if (attendance.ConfirmationExpiresAt.HasValue &&
                DateTime.UtcNow > attendance.ConfirmationExpiresAt.Value)
                return ApiResponse<string>.Fail("Token expired");

            // Only set join time here
            attendance.JoinTime ??= DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ApiResponse<string>.Success(
                attendance.Meeting.ZoomUrl ?? "",
                "Join successful"
            );
        }

        public async Task<ApiResponse<bool>> CloseMeetingAsync(int meetingId)
        {
            var meeting = await _db.Meetings
                .FirstOrDefaultAsync(m => m.MeetingId == meetingId);

            if (meeting == null)
                return ApiResponse<bool>.Fail("Meeting not found");

            if (!meeting.IsActive)
                return ApiResponse<bool>.Fail("Meeting already closed");

            // Close meeting
            meeting.IsActive = false;
            meeting.ClosedAt = DateTime.UtcNow;

            // Get attendees who actually joined
            var attendees = await _db.Attendance
                .Where(a =>
                    a.MeetingId == meetingId &&
                    a.JoinTime != null &&
                    a.AttendedVia == AttendanceChannel.Virtual)
                .ToListAsync();

            // Set confirmation token and expiry for each attendee
            foreach (var attendee in attendees)
            {
                if (string.IsNullOrWhiteSpace(attendee.ConfirmationToken))
                    attendee.ConfirmationToken = Guid.NewGuid().ToString();

                attendee.ConfirmationExpiresAt = DateTime.UtcNow.AddMinutes(15);
            }

            await _db.SaveChangesAsync();
            foreach (var attendee in attendees)
            {
                try
                {
                    var confirmLink = $"http://207.180.246.69:7200/api/attendance/confirm?token={attendee.ConfirmationToken}";
                    //var confirmLink = $"https://localhost:7067/api/attendance/confirm?token={attendee.ConfirmationToken}";//

                    var body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; background:#f6f8fb; padding:30px;'>

                            <div style='max-width:600px; margin:auto; background:white; padding:30px; border-radius:8px;'>

                                <h2 style='color:#2d8cff;'>Attendance Confirmation</h2>

                                <p>Hello,</p>

                                <p>
                                    Thank you for attending the meeting:
                                </p>

                                <h3 style='margin-bottom:10px;'>{meeting.Title}</h3>

                                <p>
                                    Kindly confirm your attendance by clicking the button below.
                                </p>

                                <p style='margin:25px 0;'>
                                    <a href='{confirmLink}'
                                        style='background:#2d8cff;
                                                color:white;
                                                padding:14px 20px;
                                                text-decoration:none;
                                                border-radius:6px;
                                                font-weight:bold;
                                                display:inline-block;'>
                                        Confirm Attendance
                                    </a>
                                </p>

                                <p style='font-size:13px; color:#666;'>
                                    This confirmation link will expire in 15 minutes.
                                </p>

                                <hr style='margin:25px 0;' />

                                <p style='margin-top:20px;'>
                                    Regards,<br/>
                                    HR Team
                                </p>

                            </div>

                        </body>
                        </html>";

                    await _emailService.SendEmailAsync(
                        attendee.StaffEmail,
                        $"Confirm Attendance — {meeting.Title}",
                        body
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Confirmation mail failed for {attendee.StaffEmail}: {ex.Message}");
                }
            }



            return ApiResponse<bool>.Success(true, "Meeting closed and confirmation emails sent");
        }

        public async Task<(bool Success, string RedirectUrl)> ConfirmCloseMeetingAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (false, "http://localhost:3000/confirmation/invalid");

            var data = await _db.Attendance
                .Where(a => a.ConfirmationToken == token)
                .Select(a => new
                {
                    a.AttendanceId,
                    a.ConfirmAttendance,
                    a.ConfirmationExpiresAt,
                    MeetingIsActive = a.Meeting.IsActive
                })
                .FirstOrDefaultAsync();

            if (data == null)
                return (false, "http://localhost:3000/confirmation/invalid");

            if (data.MeetingIsActive)
                return (false, "http://localhost:3000/confirmation/invalid");

            if (data.ConfirmAttendance)
                return (false, "http://localhost:3000/confirmation/already");

            if (data.ConfirmationExpiresAt == null ||
                data.ConfirmationExpiresAt < DateTime.UtcNow)
                return (false, "http://localhost:3000/confirmation/expired");

            // now update separately

            var attendance = await _db.Attendance.FindAsync(data.AttendanceId);

            attendance.ConfirmAttendance = true;
            attendance.ConfirmationTime = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, "http://localhost:3000/confirmation/success");
        }

        public async Task<ApiResponse<string>> GenerateAndSendLinkAsync(int meetingId, string staffEmail)
        {
            try
            {
                var meeting = await _db.Meetings
                    .FirstOrDefaultAsync(m => m.MeetingId == meetingId && m.IsActive);

                if (meeting == null)
                    return ApiResponse<string>.Fail("Meeting not found or inactive");

                var joinToken = Guid.NewGuid().ToString();
                var confirmationToken = Guid.NewGuid().ToString();

                var attendance = new MeetingAttendance
                {
                    MeetingId = meetingId,
                    StaffEmail = staffEmail,

                    JoinToken = joinToken,
                    //JoinTime = DateTime.UtcNow,
                    AttendedVia = AttendanceChannel.Virtual
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
                StaffName = staff.StaffName,
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
                    StaffName = a.Staff.StaffName,
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
                .OrderBy(s => s.StaffName)
                .ToListAsync();

            if (selectedStaff.Count == 0)
                return ApiResponse<SendQrCodeResponse>.Fail("None of the selected staff emails were found.");

            var results = new List<QrCodeEmailResult>();

            foreach (var staff in selectedStaff)
            {
                var result = new QrCodeEmailResult
                {
                    StaffId = staff.Id,
                    StaffName = staff.StaffName,
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

        public async Task<ApiResponse<List<string>>> GetAllStaffEmailsAsync()
        {
            var emails = await _db.Users
                .Select(u => u.Email)
                .ToListAsync();

            if (!emails.Any())
                return ApiResponse<List<string>>.Fail("No staff emails found");

            return ApiResponse<List<string>>.Success(emails, "Staff emails retrieved successfully");
        }
    }
}