namespace ZoomAttendance.Services
{
    public interface IZoomService
    {
        Task<(string MeetingId, string JoinUrl, string StartUrl)> CreateMeetingAsync( string title,
            DateTime startDatetime,
            int durationMinutes);

        Task<(string RegistrantId, string JoinUrl)> CreateRegistrantAsync(
            string zoomMeetingId,
            string firstName,
            string email);

        Task UpdateMeetingAsync(
            string zoomMeetingId,
            string title,
            DateTime startDatetime,
            int durationMinutes);
    }
}
