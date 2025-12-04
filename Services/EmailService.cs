using ArWidgetApi.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

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
                // Nadawca (Od)
                message.From.Add(new MailboxAddress("InteliCore Formularz", _emailSettings.FromEmail)); 
                // Odbiorca (Do)
                message.To.Add(new MailboxAddress("Tomasz Sikora", _emailSettings.ToEmail)); 
                
                // Odpowiedź trafi do klienta (ważne!)
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
                    // Używamy bezpiecznego połączenia TLS/SSL
                    await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, SecureSocketOptions.SslOnConnect); 
                    
                    // Uwierzytelnienie
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

    // Pamiętaj, aby stworzyć też ten model!
    public class ContactFormData
    {
        public string Imie { get; set; }
        public string Email { get; set; }
        public string Firma { get; set; }
        public string Wiadomosc { get; set; }
    }
}
