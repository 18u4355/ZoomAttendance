// Services/IEmailService.cs

namespace ZoomAttendance.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendAttendanceLinkEmailAsync(
            ZoomAttendance.Entities.Staff staff,
            ZoomAttendance.Entities.Meeting meeting,
            string token);
    }
}