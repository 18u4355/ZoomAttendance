using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ZoomAttendance.Data;
using ZoomAttendance.Models;
using ZoomAttendance.Models.Entities;
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

        // ✅ Generate Join Token and Send Direct Backend Link
        public async Task<ApiResponse<string>> GenerateJoinTokenAsync(int meetingId, string staffEmail)
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
                    JoinTime = DateTime.UtcNow,





                };

                _db.Attendance.Add(attendance);
                await _db.SaveChangesAsync();

                //  Direct backend link (NOT frontend)
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

        // Validate Token + Confirm + Return Zoom URL
        public async Task<ApiResponse<string>> ValidateAndConfirmAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ApiResponse<string>.Fail("Token is required");

            var attendance = await _db.Attendance
                .Include(a => a.Meeting)
                .FirstOrDefaultAsync(a => a.JoinToken == token);

            if (attendance == null)
                return ApiResponse<string>.Fail("Invalid token");

            if (attendance.ConfirmAttendance)
                return ApiResponse<string>.Fail("Attendance already confirmed");

            if (!attendance.Meeting.IsActive || attendance.Meeting.ClosedAt != null)
                return ApiResponse<string>.Fail("Meeting has been closed");

            if (attendance.ConfirmationExpiresAt.HasValue &&
                DateTime.UtcNow > attendance.ConfirmationExpiresAt.Value)
                return ApiResponse<string>.Fail("Token has expired");


            await _db.SaveChangesAsync();

            return ApiResponse<string>.Success(
                attendance.Meeting.ZoomUrl,
                "Attendance confirmed"
            );
        }

        // ✅ Close Meeting
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
                .Where(a => a.MeetingId == meetingId && a.JoinTime != null)
                .ToListAsync();

            // Set confirmation token and expiry for each attendee
            foreach (var attendee in attendees)
            {
                if (string.IsNullOrWhiteSpace(attendee.ConfirmationToken))
                    attendee.ConfirmationToken = Guid.NewGuid().ToString();

                attendee.ConfirmationExpiresAt = DateTime.UtcNow.AddMinutes(15);
            }

            // Save changes before sending emails
            await _db.SaveChangesAsync();

            // Send confirmation emails
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
                return (false, "http://localhost:3000/confirmation/invalid"); // frontend dev

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
    }
}
        //public async Task<ApiResponse<PaginatedResponse<AttendanceReportResponse>>> GetAttendanceReportAsync(AttendanceReportRequest request)
        //{
        //    try
        //    {
        //        var query = _db.Attendance
        //            .Include(a => a.Meeting)
        //            .Where(a => a.Meeting.ClosedAt != null &&
        //                        a.Meeting.ClosedAt >= request.Start &&
        //                        a.Meeting.ClosedAt <= request.End &&
        //                        a.ConfirmAttendance); // Only confirmed attendees

//        // Apply optional filters
//        if (!string.IsNullOrEmpty(request.MeetingTitle))
//            query = query.Where(a => a.Meeting.Title.Contains(request.MeetingTitle));

//        if (!string.IsNullOrEmpty(request.StaffEmail))
//            query = query.Where(a => a.StaffEmail.Contains(request.StaffEmail));

//        var totalCount = await query.CountAsync();

//        var items = await query
//            .OrderByDescending(a => a.JoinTime)
//            .Skip((request.Page - 1) * request.PageSize)
//            .Take(request.PageSize)
//            .Select(a => new AttendanceReportResponse
//            {
//                MeetingTitle = a.Meeting.Title,
//                StaffName = a.StaffEmail,
//                JoinTime = a.JoinTime ?? DateTime.MinValue,
//                ConfirmedAttendance = a.ConfirmAttendance,
//                ConfirmationTime = a.ConfirmationTime
//            })
//            .ToListAsync();

//        var result = new PaginatedResponse<AttendanceReportResponse>
//        {
//            Items = items,
//            TotalCount = totalCount,
//            Page = request.Page,
//            PageSize = request.PageSize
//        };

//        return ApiResponse<PaginatedResponse<AttendanceReportResponse>>.Success(result, "Attendance report fetched successfully");
//    }
//    catch (Exception ex)
//    {
//        return ApiResponse<PaginatedResponse<AttendanceReportResponse>>.Fail("Failed to fetch attendance report", ex.Message);
//    }
//}

