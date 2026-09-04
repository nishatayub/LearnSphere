using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LearnSphere.Services
{
    /// <summary>
    /// Sends real email over SMTP using MailKit rather than System.Net.Mail.SmtpClient -
    /// the legacy SmtpClient has long-standing TLS/socket issues connecting to Gmail from
    /// Linux containers. Only active when SmtpUsername/SmtpPassword are configured (see
    /// Program.cs) - otherwise the app falls back to NullEmailSender so local development
    /// needs no SMTP account.
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
            var message = new MimeMessage();
            // Gmail (and most SMTP providers) reject a From address that isn't the
            // authenticated account, so use SmtpUsername rather than SenderEmail.
            message.From.Add(new MailboxAddress(_options.SenderName, _options.SmtpUsername));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.SmtpUsername, _options.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Don't let a mail delivery failure break the request that triggered it
                // (e.g. approving a course) - log it and move on.
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            }
        }
    }
}
