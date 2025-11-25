namespace ArWidgetApi.Models
{
    public class ModelDataDto
    {
        // Pola mapowane z bazy danych
        public int ProductId { get; set; }
        public string Name { get; set; } 
        public string ModelUrlGlb { get; set; } 
        public string ModelUrlUsdz { get; set; }
        
        // Pola na generowane Signed URL (zwracane do frontendu)
        public string SignedUrlGlb { get; set; }
        public string SignedUrlUsdz { get; set; } 
    }
}
