namespace ZoomAttendance.Services
{
    public interface IZoomService
    {
        Task<(string MeetingId, string JoinUrl, string StartUrl)> CreateMeetingAsync( string title,
            DateTime startDatetime,
            int durationMinutes);

        Task UpdateMeetingAsync(
            string zoomMeetingId,
            string title,
            DateTime startDatetime,
            int durationMinutes);
    }
}