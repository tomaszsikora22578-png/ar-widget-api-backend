using ArWidgetApi.Models;
using ArWidgetApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArWidgetApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(IEmailService emailService, ILogger<ContactController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitContactForm([FromBody] ContactFormData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Opcjonalna walidacja danych (np. CAPTCHA, sprawdzenie spamu)
            
            _logger.LogInformation("Otrzymano zgłoszenie od {Email}", data.Email);
            
            var success = await _emailService.SendContactFormEmailAsync(data);

            if (success)
            {
                return Ok(new { success = true, message = "Wiadomość wysłana pomyślnie!" });
            }
            else
            {
                return StatusCode(500, new { success = false, message = "Wystąpił błąd podczas wysyłki e-maila." });
            }
        }
    }
}
