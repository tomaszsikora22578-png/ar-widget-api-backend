using ArWidgetApi.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.ComponentModel.DataAnnotations;
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
}
    // Pamiętaj, aby stworzyć też ten model!
   public class ContactFormData
{
    // Ograniczenie do 100 znaków (Imię i Nazwisko)
    [Required(ErrorMessage = "Pole Imię i Nazwisko jest wymagane.")]
    [MaxLength(30, ErrorMessage = "Imię i Nazwisko może zawierać maksymalnie 100 znaków.")]
    public string Imie { get; set; }

    // Wymagany i musi być poprawnym formatem email
    [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
    [MaxLength(20, EmailAddress(ErrorMessage = "Proszę podać poprawny adres e-mail.")]
    public string Email { get; set; }

    // Ograniczenie do 100 znaków (Nazwa Firmy)
    [Required(ErrorMessage = "Pole Nazwa Sklepu / Firmy jest wymagane.")]
    [MaxLength(50, ErrorMessage = "Nazwa Firmy może zawierać maksymalnie 100 znaków.")]
    public string Firma { get; set; }

    // Ograniczenie do 500 znaków (Wiadomość)
    [Required(ErrorMessage = "Pole Wiadomość jest wymagane.")]
    [MaxLength(600, ErrorMessage = "Wiadomość może zawierać maksymalnie 500 znaków.")]
    public string Wiadomosc { get; set; }
}

