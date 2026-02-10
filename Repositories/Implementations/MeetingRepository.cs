using Microsoft.EntityFrameworkCore;
using ZoomAttendance.Data;
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
                var meeting = new Meeting
                {
                    Title = request.Title,
                    ZoomUrl = request.ZoomUrl,
                    StartTime = request.StartTime,
                    CreatedBy = hrId,
                    IsActive = true
                };

                await _db.Meetings.AddAsync(meeting);
                await _db.SaveChangesAsync();
                return ApiResponse<bool>.Success(true, "Meeting created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail("Failed to create meeting", ex.Message);
            }
        }

        public async Task<ApiResponse<List<MeetingResponse>>> GetActiveMeetingsAsync()
        {
            try
            {
                var meetings = await _db.Meetings
                    .Where(m => m.IsActive)
                    .Select(m => new MeetingResponse
                    {
                        MeetingId = m.MeetingId,
                        Title = m.Title,
                        ZoomUrl = m.ZoomUrl,
                        StartTime = m.StartTime,
                        IsActive = m.IsActive
                    })
                    .ToListAsync();

                return ApiResponse<List<MeetingResponse>>.Success(meetings);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<MeetingResponse>>.Fail("Failed to fetch active meetings", ex.Message);
            }
        }

    }
}