using ArWidgetApi.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using ArWidgetApi;


namespace ArWidgetApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendContactFormEmailAsync(ContactFormData data)
        {
            try
            {
                var message = new MimeMessage();

                message.From.Add(new MailboxAddress("InteliCore Formularz", _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("Tomasz Sikora", _emailSettings.ToEmail));

                message.ReplyTo.Add(new MailboxAddress(data.Imie, data.Email));

                message.Subject = $"NOWE ZAPYTANIE (ASP.NET): {data.Imie} / {data.Firma}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <p>Otrzymano nowe zapytanie z aplikacji:</p>
                        <hr>
                        <p><strong>Imię i Nazwisko:</strong> {data.Imie}</p>
                        <p><strong>E-mail:</strong> {data.Email}</p>
                        <p><strong>Firma:</strong> {data.Firma}</p>
                        <hr>
                        <p><strong>Wiadomość:</strong></p>
                        <p>{data.Wiadomosc.Replace("\n", "<br>")}</p>
                        <hr>",
                    TextBody = $"Zapytanie od: {data.Imie}. Firma: {data.Firma}. Wiadomość: {data.Wiadomosc}"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        _emailSettings.SmtpHost,
                        _emailSettings.SmtpPort,
                        SecureSocketOptions.SslOnConnect);

                    await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation("E-mail wysłany pomyślnie.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłania e-maila przez SMTP.");
                return false;
            }
        }
    }

    // 🔥 MODEL NAPRAWIONY i WEWNĄTRZ NAMESPACE!
    public class ContactFormData
    {
        [Required]
        [MaxLength(30)]
        public string Imie { get; set; }

        [Required]
        [EmailAddress]           // ← poprawnie
        [MaxLength(50)]          // ← poprawnie
        public string Email { get; set; }

        [Required]
        [MaxLength(50)]
        public string Firma { get; set; }

        [Required]
        [MaxLength(600)]
        public string Wiadomosc { get; set; }
    }
}
