using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceAbstraction;

namespace Presistence
{
    public class EmailSettings
    {
        public const string SectionName = "Email";
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = "noreply@fitmate.com";
        public string FromName { get; set; } = "FitMate";
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendPasswordResetCodeAsync(string email, string code)
        {
            var subject = "FitMate — Password Reset Code";
            var body =
                $"Your FitMate password reset verification code is: {code}\n\n" +
                $"This code expires in 15 minutes.\n\n" +
                "If you did not request this, you can ignore this email.";

            if (string.IsNullOrWhiteSpace(_settings.SmtpHost) ||
                string.IsNullOrWhiteSpace(_settings.Username))
            {
                _logger.LogWarning(
                    "Email SMTP is not configured. Password reset code for {Email}: {Code}",
                    email,
                    code);
                return;
            }

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };
            message.To.Add(email);

            await client.SendMailAsync(message);
        }
    }
}
