using System.Net;
using System.Net.Mail;
using ZoomAttendance.Services;

namespace ZoomAttendance.Services
{

    public class EmailService : IEmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var fromEmail = "amurtala@softworksng.com"; // Your Zoho email
                var fromPassword = "92RPTKeHACqc\r\n"; // App password if 2FA is enabled

                var mail = new MailMessage();
                mail.From = new MailAddress(fromEmail);
                mail.To.Add(toEmail);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                using var smtp = new SmtpClient("smtp.zoho.com", 587); // TLS
                smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
                smtp.EnableSsl = true;

                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                throw;
            }
        }
       
        }

    }