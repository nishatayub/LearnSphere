using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace LearnSphere.Services
{
    /// <summary>
    /// Sends email through Resend's HTTP API rather than raw SMTP - free hosts like
    /// Render commonly block outbound SMTP ports, but HTTPS (443) always works. Only
    /// active when an ApiKey is configured (see Program.cs) - otherwise the app falls
    /// back to NullEmailSender so local development needs no Resend account.
    /// </summary>
    public class ResendEmailSender : IEmailSender
    {
        private readonly HttpClient _httpClient;
        private readonly EmailOptions _options;
        private readonly ILogger<ResendEmailSender> _logger;

        public ResendEmailSender(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<ResendEmailSender> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "emails")
            {
                Content = JsonContent.Create(new
                {
                    from = $"{_options.SenderName} <{_options.SenderEmail}>",
                    to = new[] { toEmail },
                    subject,
                    html = htmlBody
                }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey) }
            };

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send email to {ToEmail}: {StatusCode} {Body}", toEmail, response.StatusCode, body);
                }
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
