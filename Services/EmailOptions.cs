namespace LearnSphere.Services
{
    public class EmailOptions
    {
        public const string SectionName = "Email";

        public string SmtpHost { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 587;

        public string SmtpUsername { get; set; } = string.Empty;

        public string SmtpPassword { get; set; } = string.Empty;

        public string SenderName { get; set; } = "LearnSphere";
    }
}
