using System.ComponentModel.DataAnnotations;

namespace ArWidgetApi.Models
{
    public class ContactFormData
    {
        // 1. Pole: Imię i Nazwisko (Wymagane)
        [Required(ErrorMessage = "Pole Imię i Nazwisko jest wymagane.")]
        public string ImieINazwisko { get; set; } = null!;

        // 2. Pole: Służbowy Email (Wymagane)
        [Required(ErrorMessage = "Pole Email jest wymagane.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email.")]
        public string EmailSluzbowy { get; set; } = null!;

        // 3. Pole: Nazwa Sklepu/Firmy (Opcjonalne - bez atrybutu [Required])
        public string NazwaFirmy { get; set; } = null!;

        // 4. Pole: Opis (Wymagane)
        [Required(ErrorMessage = "Pole Opis/Wiadomość jest wymagane.")]
        public string Opis { get; set; } = null!;
    }
}
