using QRCoder;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using ZoomAttendance.Entities;
using ZoomAttendance.Models.Entities;

namespace ZoomAttendance.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _fromEmail = "amurtala@softworksng.com";
        private readonly string _fromPassword = "92RPTKeHACqc";
        private readonly string _smtpHost = "smtp.zoho.com";
        private readonly int _smtpPort = 587;

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress(_fromEmail);
                mail.To.Add(toEmail);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                using var smtp = new SmtpClient(_smtpHost, _smtpPort);
                smtp.Credentials = new NetworkCredential(_fromEmail, _fromPassword);
                smtp.EnableSsl = true;

                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                throw;
            }
        }

        // ── QR code email — uses LinkedResource so image shows in all clients ─
        public async Task SendQrCodeEmailAsync(Staff staff, Meeting meeting)
        {
            // 1. Generate QR code bytes from BarcodeToken
            byte[] qrBytes = GenerateQrCodeImage(staff.BarcodeToken);

            // 2. Build HTML referencing the image via cid (content ID)
            var htmlBody = $@"
                <html>
                <body style=""font-family: Arial, sans-serif; color: #333;"">
                    <h2>Hello {staff.FullName},</h2>
                    <p>Please find your personal QR code below for the upcoming meeting:</p>
                    <p>
                        <strong>{meeting.Title}</strong><br/>
                        Date: {meeting.CreatedAt:dddd, dd MMMM yyyy} at {meeting.CreatedAt:HH:mm}
                    </p>
                    <p>Present this QR code at the entrance to record your physical attendance.</p>
                    <br/>
                    <img src=""cid:qrcode"" alt=""Your QR Code"" width=""250"" height=""250"" />
                    <br/><br/>
                    <p style=""color: #999; font-size: 12px;"">
                        This QR code is unique to you. Do not share it with anyone.
                    </p>
                </body>
                </html>";

            // 3. Create LinkedResource from QR bytes — this is what makes it show in email clients
            var qrResource = new LinkedResource(new MemoryStream(qrBytes), "image/png");
            qrResource.ContentId = "qrcode";
            qrResource.TransferEncoding = TransferEncoding.Base64;

            // 4. Build AlternateView with the linked resource attached
            var htmlView = AlternateView.CreateAlternateViewFromString(
                htmlBody,
                null,
                "text/html"
            );
            htmlView.LinkedResources.Add(qrResource);

            // 5. Build and send the mail message
            var mail = new MailMessage();
            mail.From = new MailAddress(_fromEmail);
            mail.To.Add(new MailAddress(staff.Email));
            mail.Subject = $"Your Attendance QR Code – {meeting.Title}";
            mail.AlternateViews.Add(htmlView);

            using var smtp = new SmtpClient(_smtpHost, _smtpPort);
            smtp.Credentials = new NetworkCredential(_fromEmail, _fromPassword);
            smtp.EnableSsl = true;

            try
            {
                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"QR email sending failed: {ex.Message}");
                throw;
            }
        }

        // ── QR Code Generator ─────────────────────────────────────────────────
        private static byte[] GenerateQrCodeImage(string token)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            return qrCode.GetGraphic(10); // 10px per module = ~250px image
        }
    }
}