//// Services/EmailService.cs

//using MailKit.Security;
//using MimeKit;
//using ZoomAttendance.Entities;
//using MailKit.Net.Smtp;

//namespace ZoomAttendance.Services
//{
//    public class EmailService : IEmailService
//    {
//        private readonly IConfiguration _config;

//        public EmailService(IConfiguration config)
//        {
//            _config = config;
//        }

//        // ── Generic send ──────────────────────────────────────────────────────


//        public async Task SendEmailAsync(string toEmail, string subject, string body)
//        {
//            var host = _config["Email:SmtpHost"]!;
//            var port = int.Parse(_config["Email:SmtpPort"]!);
//            var user = _config["Email:SmtpUser"]!;
//            var pass = _config["Email:SmtpPass"]!;

//            var email = new MimeMessage();
//            email.From.Add(new MailboxAddress("MeetTrack HR", user));
//            email.To.Add(MailboxAddress.Parse(toEmail));
//            email.Subject = subject;
//            email.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

//            using var client = new SmtpClient();
//            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
//            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
//            await client.AuthenticateAsync(user, pass);
//            await client.SendAsync(email);
//            await client.DisconnectAsync(true);

//        }

//        // ── Attendance invite email (mode-aware) ──────────────────────────────
//        public async Task SendAttendanceLinkEmailAsync(Staff staff, Meeting meeting, string token)
//        {
//            var baseUrl = _config["AppSettings:BaseUrl"]!;
//            var confirmLink = $"{baseUrl}/attendance/confirm?token={token}";
//            var joinLink = $"{baseUrl}/api/v1/meeting-invites/attendance/join?token={token}";

//            var linksHtml = meeting.Mode switch
//            {
//                "physical" => $@"
//                    <p style='margin:25px 0;'>
//                        <a href='{confirmLink}'
//                           style='background:#2d8cff;color:white;padding:14px 28px;
//                                  text-decoration:none;border-radius:6px;
//                                  font-weight:bold;display:inline-block;'>
//                            Confirm Attendance
//                        </a>
//                    </p>
//                    <p style='font-size:13px;color:#e74c3c;font-weight:bold;'>
//                        ⚠ Your GPS location will be verified when you click this link.
//                        Please only click when you are physically at the meeting venue.
//                    </p>",

//                "virtual" => $@"
//                    <p style='margin:25px 0;'>
//                        <a href='{joinLink}'
//                           style='background:#00b96b;color:white;padding:14px 28px;
//                                  text-decoration:none;border-radius:6px;
//                                  font-weight:bold;display:inline-block;'>
//                            Join Zoom Meeting
//                        </a>
//                    </p>",

//                _ /* hybrid */ => $@"
//                    <p><strong>Physical Attendance:</strong></p>
//                    <p style='margin:15px 0;'>
//                        <a href='{confirmLink}'
//                           style='background:#2d8cff;color:white;padding:14px 28px;
//                                  text-decoration:none;border-radius:6px;
//                                  font-weight:bold;display:inline-block;'>
//                            Confirm Physical Attendance
//                        </a>
//                    </p>
//                    <p style='font-size:13px;color:#e74c3c;font-weight:bold;'>
//                        ⚠ GPS location will be verified for physical attendance.
//                    </p>
//                    <p style='margin-top:20px;'><strong>Virtual Attendance:</strong></p>
//                    <p style='margin:15px 0;'>
//                        <a href='{joinLink}'
//                           style='background:#00b96b;color:white;padding:14px 28px;
//                                  text-decoration:none;border-radius:6px;
//                                  font-weight:bold;display:inline-block;'>
//                            Join Zoom Meeting
//                        </a>
//                    </p>"
//            };

//            var subject = $"Meeting Invitation – {meeting.Title}";
//            var body = $@"
//                <html>
//                <body style='font-family:Arial,sans-serif;color:#333;background:#f6f8fb;padding:30px;'>
//                    <div style='max-width:600px;margin:auto;background:white;padding:30px;border-radius:8px;'>
//                        <h2 style='color:#2d8cff;'>Hello {staff.Name},</h2>
//                        <p>You have been invited to the following meeting:</p>
//                        <table style='width:100%;border-collapse:collapse;margin:15px 0;'>
//                            <tr>
//                                <td style='padding:8px;font-weight:bold;color:#555;width:140px;'>Meeting</td>
//                                <td style='padding:8px;'>{meeting.Title}</td>
//                            </tr>
//                            <tr style='background:#f9f9f9;'>
//                                <td style='padding:8px;font-weight:bold;color:#555;'>Date &amp; Time</td>
//                                <td style='padding:8px;'>{meeting.StartDatetime:dddd, MMMM dd yyyy} at {meeting.StartDatetime:HH:mm} UTC</td>
//                            </tr>
//                            <tr>
//                                <td style='padding:8px;font-weight:bold;color:#555;'>Duration</td>
//                                <td style='padding:8px;'>{meeting.DurationMinutes} minutes</td>
//                            </tr>
//                            <tr style='background:#f9f9f9;'>
//                                <td style='padding:8px;font-weight:bold;color:#555;'>Mode</td>
//                                <td style='padding:8px;'>{meeting.Mode}</td>
//                            </tr>
//                        </table>
//                        {linksHtml}
//                        <p style='font-size:13px;color:#666;margin-top:20px;'>
//                            This link is unique to you. Do not share it with anyone.
//                        </p>
//                        <hr style='margin:25px 0;border:none;border-top:1px solid #eee;'/>
//                        <p style='color:#888;font-size:13px;'>Regards,<br/>HR Team</p>
//                    </div>
//                </body>
//                </html>";

//            await SendEmailAsync(staff.Email, subject, body);
//        }
//    }
//}


using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using ZoomAttendance.Entities;

namespace ZoomAttendance.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public EmailService(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var host = _config["Email:SmtpHost"]!;
            var port = int.Parse(_config["Email:SmtpPort"]!);
            var user = _config["Email:SmtpUser"]!;
            var pass = _config["Email:SmtpPass"]!;

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("MeetTrack HR", user));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            using var client = new SmtpClient { Timeout = 70000 };

            // bypass SSL cert check in dev only
            if (_env.IsDevelopment())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            }

            // auto pick SSL mode based on port
            SecureSocketOptions secure = port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(host, port, secure);
            await client.AuthenticateAsync(user, pass);
            await client.SendAsync(email);
            await client.DisconnectAsync(true);
        }

        // rest of your code stays exactly the same...
        // ── Attendance invite email(mode - aware) ──────────────────────────────
        public async Task SendAttendanceLinkEmailAsync(Staff staff, Meeting meeting, string token, string? virtualJoinLink = null)
        {
            var baseUrl = _config["AppSettings:BaseUrl"]!;
            var confirmLink = $"{baseUrl}/api/v1/meeting-invites/attendance/confirm?token={token}";
            var joinLink = !string.IsNullOrWhiteSpace(virtualJoinLink)
                ? virtualJoinLink
                : $"{baseUrl}/api/v1/meeting-invites/attendance/join?token={token}";

            var linksHtml = meeting.Mode switch
            {
                "physical" => $@"
                            <p style='margin:25px 0;'>
                                <a href='{confirmLink}'
                                   style='background:#2d8cff;color:white;padding:14px 28px;
                                          text-decoration:none;border-radius:6px;
                                          font-weight:bold;display:inline-block;'>
                                    Confirm Attendance
                                </a>
                            </p>
                            <p style='font-size:13px;color:#e74c3c;font-weight:bold;'>
                                ⚠ Your GPS location will be verified when you click this link.
                                Please only click when you are physically at the meeting venue.
                            </p>",

                "virtual" => $@"
                            <p style='margin:25px 0;'>
                                <a href='{joinLink}'
                                   style='background:#00b96b;color:white;padding:14px 28px;
                                          text-decoration:none;border-radius:6px;
                                          font-weight:bold;display:inline-block;'>
                                    Join Zoom Meeting
                                </a>
                            </p>
                            <p style='font-size:13px;color:#8a6d3b;font-weight:bold;'>
                                Use your official staff email address on Zoom. Virtual attendance is matched with your Zoom email.
                            </p>",

                _ /* hybrid */ => $@"
                            <p><strong>Physical Attendance:</strong></p>
                            <p style='margin:15px 0;'>
                                <a href='{confirmLink}'
                                   style='background:#2d8cff;color:white;padding:14px 28px;
                                          text-decoration:none;border-radius:6px;
                                          font-weight:bold;display:inline-block;'>
                                    Confirm Physical Attendance
                                </a>
                            </p>
                            <p style='font-size:13px;color:#e74c3c;font-weight:bold;'>
                                ⚠ GPS location will be verified for physical attendance.
                            </p>
                            <p style='margin-top:20px;'><strong>Virtual Attendance:</strong></p>
                            <p style='margin:15px 0;'>
                                <a href='{joinLink}'
                                   style='background:#00b96b;color:white;padding:14px 28px;
                                          text-decoration:none;border-radius:6px;
                                          font-weight:bold;display:inline-block;'>
                                    Join Zoom Meeting
                                </a>
                            </p>
                            <p style='font-size:13px;color:#8a6d3b;font-weight:bold;'>
                                If you are attending virtually, use your official staff email address on Zoom so your attendance can be matched correctly.
                            </p>"
            };

            var subject = $"Meeting Invitation – {meeting.Title}";
            var body = $@"
                        <html>
                        <body style='font-family:Arial,sans-serif;color:#333;background:#f6f8fb;padding:30px;'>
                            <div style='max-width:600px;margin:auto;background:white;padding:30px;border-radius:8px;'>
                                <h2 style='color:#2d8cff;'>Hello {staff.Name},</h2>
                                <p>You have been invited to the following meeting:</p>
                                <table style='width:100%;border-collapse:collapse;margin:15px 0;'>
                                    <tr>
                                        <td style='padding:8px;font-weight:bold;color:#555;width:140px;'>Meeting</td>
                                        <td style='padding:8px;'>{meeting.Title}</td>
                                    </tr>
                                    <tr style='background:#f9f9f9;'>
                                        <td style='padding:8px;font-weight:bold;color:#555;'>Date &amp; Time</td>
                                        <td style='padding:8px;'>{meeting.StartDatetime:dddd, MMMM dd yyyy} at {meeting.StartDatetime:HH:mm} UTC</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:8px;font-weight:bold;color:#555;'>Duration</td>
                                        <td style='padding:8px;'>{meeting.DurationMinutes} minutes</td>
                                    </tr>
                                    <tr style='background:#f9f9f9;'>
                                        <td style='padding:8px;font-weight:bold;color:#555;'>Mode</td>
                                        <td style='padding:8px;'>{meeting.Mode}</td>
                                    </tr>
                                    {(string.IsNullOrWhiteSpace(meeting.VenueName) ? string.Empty : $@"
                                    <tr>
                                        <td style='padding:8px;font-weight:bold;color:#555;'>Venue</td>
                                        <td style='padding:8px;'>{meeting.VenueName}</td>
                                    </tr>")}
                                </table>
                                {linksHtml}
                                <p style='font-size:13px;color:#666;margin-top:20px;'>
                                    This link is unique to you. Do not share it with anyone.
                                </p>
                                <hr style='margin:25px 0;border:none;border-top:1px solid #eee;'/>
                                <p style='color:#888;font-size:13px;'>Regards,<br/>HR Team</p>
                            </div>
                        </body>
                        </html>";

            await SendEmailAsync(staff.Email, subject, body);
        }
    }
}
