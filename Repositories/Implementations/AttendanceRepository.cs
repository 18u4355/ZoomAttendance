using Microsoft.EntityFrameworkCore;
using ZoomAttendance.Data;
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

        //  Generate join token and send email to staff
        public async Task<ApiResponse<string>> GenerateJoinTokenAsync(int meetingId, string staffEmail)
        {
            try
            {
                // Check meeting exists
                var meeting = await _db.Meetings.FirstOrDefaultAsync(m => m.MeetingId == meetingId);
                if (meeting == null)
                    return ApiResponse<string>.Fail("Meeting not found");

                // Create attendance record
                var token = Guid.NewGuid().ToString();
                var attendance = new MeetingAttendance
                {
                    MeetingId = meetingId,
                    StaffEmail = staffEmail,
                    JoinToken = token,
                    ConfirmAttendance = false,
                    JoinTime = null,
                    ConfirmationTime = null
                };

                _db.Attendance.Add(attendance);
                await _db.SaveChangesAsync();

                // Send email with join link
                var joinLink = $"https://yourfrontend.com/join?token={token}";
                await _emailService.SendEmailAsync(
                    staffEmail,
                    "Meeting Invitation",
                    $"You are invited to join the meeting: <a href='{joinLink}'>Join Meeting</a>"
                );

                return ApiResponse<string>.Success(token, "Token generated and email sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
                return ApiResponse<string>.Fail("Failed to generate token or send email", ex.InnerException?.Message ?? ex.Message);
            }
        }

        // Staff joins with token
        public async Task<ApiResponse<bool>> LogAttendanceAsync(string token)
        {
            var record = await _db.Attendance.FirstOrDefaultAsync(a => a.JoinToken == token);
            if (record == null) return ApiResponse<bool>.Fail("Invalid token");

            record.JoinTime = DateTime.UtcNow;
            record.ConfirmAttendance = false; // confirmed only after meeting closes
            await _db.SaveChangesAsync();

            return ApiResponse<bool>.Success(true, "Attendance logged successfully");
        }


        public async Task<ApiResponse<bool>> ConfirmAttendanceAsync(string token)
        {
            var record = await _db.Attendance.FirstOrDefaultAsync(a => a.JoinToken == token);
            if (record == null) return ApiResponse<bool>.Fail("Invalid token");

            if (!record.ConfirmationExpiresAt.HasValue || DateTime.UtcNow > record.ConfirmationExpiresAt.Value)
            {
                return ApiResponse<bool>.Fail("Confirmation window expired");
            }

            record.ConfirmAttendance = true;
            record.ConfirmationTime = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return ApiResponse<bool>.Success(true, "Attendance confirmed successfully");
        }

        public async Task<ApiResponse<bool>> CloseMeetingAsync(int meetingId)
        {
            var meeting = await _db.Meetings.FirstOrDefaultAsync(m => m.MeetingId == meetingId);
            if (meeting == null) return ApiResponse<bool>.Fail("Meeting not found");
            if (!meeting.IsActive) return ApiResponse<bool>.Fail("Meeting already closed");

            meeting.IsActive = false;
            meeting.ClosedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Send confirmation emails to all who joined
            var attendees = await _db.Attendance
                .Where(a => a.MeetingId == meetingId && a.JoinTime != null)
                .ToListAsync();

            foreach (var attendee in attendees)
            {
                attendee.ConfirmationExpiresAt = meeting.ClosedAt.Value.AddMinutes(15);

                var confirmLink = $"https://yourfrontend.com/confirm?token={attendee.JoinToken}";
                await _emailService.SendEmailAsync(
                    attendee.StaffEmail,
                    "Confirm Your Attendance",
                    $"Please confirm your attendance for '{meeting.Title}'. This link will expire in 15 minutes: <a href='{confirmLink}'>Confirm Attendance</a>"
                );
            }

            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Success(true, "Meeting closed and confirmation emails sent");
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

    }
}