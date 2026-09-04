using LearnSphere.Services;

namespace LearnSphere.Tests
{
    /// <summary>
    /// Always reports IsConfigured = false so controller tests never attempt a real
    /// SMTP connection, and records what would have been sent for tests that care.
    /// </summary>
    internal sealed class NullEmailSenderForTests : IEmailSender
    {
        public List<(string ToEmail, string Subject, string HtmlBody)> SentEmails { get; } = new();

        public bool IsConfigured { get; set; }

        public Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            SentEmails.Add((toEmail, subject, htmlBody));
            return Task.CompletedTask;
        }
    }
}
