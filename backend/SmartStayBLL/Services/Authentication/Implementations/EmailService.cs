using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace SmartStayBLL
{
    public sealed class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendOtpAsync(
            string email,
            string code,
            CancellationToken cancellationToken = default)
        {
            using var smtpClient = new SmtpClient
            {
                Host = _settings.Host,
                Port = _settings.Port,
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(
                    _settings.Username,
                    _settings.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(
                    _settings.Username,
                    _settings.FromName),

                Subject = "SmartStay verification code",

                Body = $"""
                        Hello,

                        Your SmartStay verification code is:

                        {code}

                        This code will expire soon.

                        If you did not request this code, you can ignore this email.
                        """,

                IsBodyHtml = false
            };

            message.To.Add(email);

            cancellationToken.ThrowIfCancellationRequested();

            await smtpClient.SendMailAsync(message);
        }
    }
}