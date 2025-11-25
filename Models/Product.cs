namespace ArWidgetApi.Models
{
    public class Product
    {
       public int Id { get; set; }
    public string ProductSku { get; set; } // SKU uzywane przez klienta
    public string Name { get; set; }

    // DODANE POLA (OK - istnieją w Twojej bazie)
    public string Description { get; set; } // Opis dla strony klienta
    public string AltText { get; set; }     // Tekst alternatywny dla AR/SEO

    // Pelne sciezki URL do modelu na Cloud Storage (OK)
    public string ModelUrlGlb { get; set; }
    public string ModelUrlUsdz { get; set; }

    // 🔴 USUNIĘTO: public int ClientId { get; set; } // FK
    // 🔴 USUNIĘTO: public Client Client { get; set; }
    // Te pola nie istnieją w tabeli Products i powodowały błąd MySqlException.

    // ✅ ZOSTAWIONO: Poprawna właściwość dla relacji WIELE-DO-WIELU:
    // Produkt jest dostępny dla wielu wpisów w tabeli Client_Product_Access
    public ICollection<ClientProductAccess> ClientProductAccess { get; set; }
    }
}
