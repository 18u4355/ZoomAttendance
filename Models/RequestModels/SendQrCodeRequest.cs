namespace ZoomAttendance.Models.RequestModels
{
    public class SendQrCodeRequest
    {
        public List<string> StaffEmails { get; set; } = new(); // HR selects from dropdown
        public int MeetingId { get; set; }
    }
}