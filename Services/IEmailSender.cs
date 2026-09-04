namespace LearnSphere.Services
{
    public interface IEmailSender
    {
        /// <summary>
        /// True when real SMTP credentials are configured. Controllers use this to
        /// decide whether to actually send mail or fall back to showing the
        /// content directly (e.g. a password reset link) for local development.
        /// </summary>
        bool IsConfigured { get; }

        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}
