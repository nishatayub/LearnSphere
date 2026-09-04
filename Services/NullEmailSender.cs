namespace LearnSphere.Services
{
    /// <summary>
    /// Used when no SMTP credentials are configured (the default for local
    /// development). Logs what would have been sent instead of actually sending -
    /// controllers check IsConfigured and fall back to showing content on-screen
    /// (e.g. the password reset link) rather than pretending an email went out.
    /// </summary>
    public class NullEmailSender : IEmailSender
    {
        private readonly ILogger<NullEmailSender> _logger;

        public NullEmailSender(ILogger<NullEmailSender> logger)
        {
            _logger = logger;
        }

        public bool IsConfigured => false;

        public Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            _logger.LogInformation("Email not sent (no SMTP configured). To: {ToEmail}, Subject: {Subject}", toEmail, subject);
            return Task.CompletedTask;
        }
    }
}
