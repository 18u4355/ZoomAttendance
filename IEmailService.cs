using ZoomAttendance.Entities;
using ZoomAttendance.Models.Entities;

namespace ZoomAttendance.Services
    {
        public interface IEmailService
        {
            Task SendEmailAsync(string toEmail, string subject, string body);
            Task SendQrCodeEmailAsync(Staff staff, Meeting meeting);
    }
    }

