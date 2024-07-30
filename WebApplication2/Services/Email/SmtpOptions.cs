namespace Imgriff.Services.Email
{
    public class SmtpOptions
    {
        public SmtpConfig Gmail { get; set; }
    }

    public class SmtpConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool Ssl { get; set; }
    }
}
