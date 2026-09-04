using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace LearnSphere.Services
{
    /// <summary>
    /// Sends real email over SMTP. Only active when SmtpUsername/SmtpPassword are
    /// configured (see EmailSenderExtensions.AddEmailSender) - otherwise the app
    /// falls back to NullEmailSender so local development needs no SMTP account.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_options.SmtpUsername) &&
            !string.IsNullOrWhiteSpace(_options.SmtpPassword);

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_options.SenderEmail, _options.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            try
            {
                await client.SendMailAsync(message);
            }
            catch (SmtpException ex)
            {
                // Don't let a mail delivery failure break the request that triggered it
                // (e.g. approving a course) - log it and move on.
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            }
        }
    }
}
