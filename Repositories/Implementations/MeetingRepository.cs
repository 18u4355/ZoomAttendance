using Microsoft.EntityFrameworkCore;
using ZoomAttendance.Data;
using ZoomAttendance.Models.Entities;
using ZoomAttendance.Models.RequestModels;
using ZoomAttendance.Models.ResponseModels;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Repositories.Implementations
{
    public class MeetingRepository : IMeetingRepository
    {
        private readonly ApplicationDbContext _db;
        public MeetingRepository(ApplicationDbContext db) => _db = db;

        public async Task<ApiResponse<bool>> CreateMeetingAsync(CreateMeetingRequest request, int hrId)
        {
            try
            {
                var meeting = new Meeting
                {
                    Title = request.Title,
                    ZoomUrl = request.ZoomUrl,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
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

        public async Task<ApiResponse<bool>> EndMeetingAsync(int meetingId)
        {
            try
            {
                var meeting = await _db.Meetings.FirstOrDefaultAsync(m => m.MeetingId == meetingId);
                if (meeting == null) return ApiResponse<bool>.Fail("Meeting not found");

                meeting.IsActive = false;
                meeting.EndTime = DateTime.UtcNow;

                _db.Meetings.Update(meeting);
                await _db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Meeting ended successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail("Failed to end meeting", ex.Message);
            }
        }
    }
}
