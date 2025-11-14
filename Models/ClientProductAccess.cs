using System.ComponentModel.DataAnnotations.Schema;

namespace ArWidgetApi.Models
{
    // Nazwa tabeli w bazie to Client_Product_Access
    [Table("Client_Product_Access")]
    public class ClientProductAccess
    {
        // Klucze obce, które tworzą klucz złożony (PK)
        public int client_id { get; set; }
        public int product_id { get; set; }

        // Właściwości nawigacyjne (wymagane przez EF Core)
        public Client Client { get; set; }
        public Product Product { get; set; }
    }
}
