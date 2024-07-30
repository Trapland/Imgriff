using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.Net.Mime;

namespace Imgriff.Services.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpOptions _smtpOptions;
        private readonly ILogger<EmailSender> _logger;
        private readonly IWebHostEnvironment _env;

        public EmailSender(IOptions<SmtpOptions> smtpOptions, ILogger<EmailSender> logger, IWebHostEnvironment env)
        {
            _smtpOptions = smtpOptions.Value;
            _logger = logger;
            _env = env;
        }

        public virtual Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using (var client = new SmtpClient(_smtpOptions.Gmail.Host, _smtpOptions.Gmail.Port))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_smtpOptions.Gmail.Email, _smtpOptions.Gmail.Password);
                client.EnableSsl = _smtpOptions.Gmail.Ssl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpOptions.Gmail.Email),
                    Subject = subject,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    HeadersEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8,
                };

                mailMessage.To.Add(email);

                try
                {
                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlMessage, null, MediaTypeNames.Text.Html);

                    string imagePath = Path.Combine(_env.WebRootPath, "images", "logoImage.jpg");
                    LinkedResource logo = new LinkedResource(imagePath, MediaTypeNames.Image.Jpeg)
                    {
                        ContentId = "logoImage"
                    };
                    htmlView.LinkedResources.Add(logo);

                    mailMessage.AlternateViews.Add(htmlView);

                    client.Send(mailMessage);
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error during sending email. {ex}");
                    throw;
                }
            }
        }
    }
}
