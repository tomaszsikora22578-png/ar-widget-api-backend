using ArWidgetApi.Models;

namespace ArWidgetApi.Services
{
    public interface IEmailService
    {
        Task<bool> SendContactFormEmailAsync(ContactFormData data);
    }
}
