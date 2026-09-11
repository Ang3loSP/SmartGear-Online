namespace SmartGear_Online.Services
{
    /// <summary>
    /// SMTP settings bound from the "Email" configuration section.
    /// Leave <c>Host</c> empty to keep email delivery disabled — the
    /// NotificationService falls back to logging only, so local/dev runs
    /// never break. Credentials should live in user-secrets or environment
    /// variables, never in source control.
    /// </summary>
    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromName { get; set; } = "SmartGear Online";
        public string FromAddress { get; set; } = "support@smartgear.com";
        public bool UseSsl { get; set; } = true;
    }
}