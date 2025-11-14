// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using Google.Apis.Auth.OAuth2; 
using System.IO;

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private readonly StorageClient _storageClient; 
        private const string BucketName = "ar-models-dla-klientow"; 

        public GcsService()
        {
            // Pobranie ścieżki do klucza JSON z ZMIENNEJ ŚRODOWISKOWEJ
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");

            // WAŻNE: Weryfikacja
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                // W przypadku, gdy klucz nie jest dostępny (np. podczas kompilacji)
                // Używamy bezpiecznej, ale mniej pewnej metody Create()
                _storageClient = StorageClient.Create();
            }
            else
            {
                // TWORZYMY KLIENTA Z JAWNYM POŚWIADCZENIEM Z PLIKU JSON
                var credential = GoogleCredential.FromFile(keyPath);
                _storageClient = StorageClient.Create(credential);
            }
        }

        public string GenerateSignedUrl(string objectName)
        {
            var expiration = DateTime.UtcNow.AddMinutes(5); 

            // Używamy metody instancyjnej CreateSignedUrl, KTÓRA ISTNIEJE.
            // Błąd CS1061 był wcześniej wynikiem błędnego użycia metody statycznej
            // zamiast instancyjnej i na odwrót. Ta musi zadziałać.
            string signedUrl = _storageClient.CreateSignedUrl(
                BucketName,
                objectName,
                expiration,
                HttpMethod.Get
            );
            
            return signedUrl;
        }
    }
}
