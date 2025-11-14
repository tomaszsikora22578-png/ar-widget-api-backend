namespace ArWidgetApi.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductSku { get; set; } // SKU uzywane przez klienta
        public string Name { get; set; }

        // DODANE POLA
        public string Description { get; set; } // Opis dla strony klienta
        public string AltText { get; set; }     // Tekst alternatywny dla AR/SEO

        // Pelne sciezki URL do modelu na Cloud Storage
        public string ModelUrlGlb { get; set; }
        public string ModelUrlUsdz { get; set; }

        public int ClientId { get; set; } // FK
        public Client Client { get; set; }
        // NOWA Właściwość nawigacyjna:
     // Produkt jest dostępny dla wielu wpisów w tabeli Client_Product_Access
       public ICollection<ClientProductAccess> ClientProductAccess { get; set; }
    }
}
