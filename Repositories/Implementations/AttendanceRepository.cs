using Microsoft.EntityFrameworkCore;
using ZoomAttendance.Data;
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
            try
            {
                var record = await _db.Attendance.FirstOrDefaultAsync(a => a.JoinToken == token);
                if (record == null) return ApiResponse<bool>.Fail("Invalid token");

                record.JoinTime = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Attendance logged successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail("Failed to log attendance", ex.Message);
            }
        }

        // ✅ Staff confirms attendance
        public async Task<ApiResponse<bool>> ConfirmAttendanceAsync(string token)
        {
            try
            {
                var record = await _db.Attendance.FirstOrDefaultAsync(a => a.JoinToken == token);
                if (record == null) return ApiResponse<bool>.Fail("Invalid token");

                record.ConfirmAttendance = true;
                record.ConfirmationTime = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Attendance confirmed");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail("Failed to confirm attendance", ex.Message);
            }
        }
       
    }
}
