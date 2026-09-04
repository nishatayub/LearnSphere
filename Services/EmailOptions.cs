namespace LearnSphere.Services
{
    public class EmailOptions
    {
        public const string SectionName = "Email";

        public string ApiKey { get; set; } = string.Empty;

        public string SenderEmail { get; set; } = "onboarding@resend.dev";

        public string SenderName { get; set; } = "LearnSphere";
    }
}
