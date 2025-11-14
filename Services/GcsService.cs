// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using System.IO; 
using Google.Apis.Auth.OAuth2; // Zostawiamy - jest kluczowe!
// USUŃ: using Google.Cloud.Storage.V1.Signing; 
// USUŃ: using Google.Apis.Auth.OAuth2.ServiceAccount; 
// USUŃ: using Google.Cloud.Storage.V1.Implementation; 

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private readonly StorageClient _storageClient;
        private const string BucketName = "ar-models-dla-klientow";
        private readonly GoogleCredential _credential;

        public GcsService()
        {
            // POBRANIE ŚCIEŻKI DO KLUCZA JSON Z Cloud Run
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
            
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                // W Cloud Run zazwyczaj używamy automatycznej autoryzacji
                _credential = GoogleCredential.GetApplicationDefault();
                _storageClient = StorageClient.Create(_credential);
            }
            else
            {
                // Wymuszenie użycia klucza z pliku JSON (najstabilniejsze)
                _credential = GoogleCredential.FromFile(keyPath);
                _storageClient = StorageClient.Create(_credential);
            }
        }

        public string GenerateSignedUrl(string objectName)
        {
            var expiration = DateTime.UtcNow.AddMinutes(5); 

            // UŻYCIE INSTANCYJNEJ METODY CreateSignedUrl 
            // Wersja, która przyjmuje ServiceAccountCredential i duration.
            
            // Konwersja na ServiceAccountCredential jest kluczowa dla podpisywania,
            // dlatego użyjemy metody, która to ukrywa i wymaga DateTimeOffset
            // (wcześniej próbowaliśmy DateTime, co też zawiodło):
            string signedUrl = _storageClient.CreateSignedUrl(
                BucketName,
                objectName,
                DateTimeOffset.UtcNow.AddMinutes(5).DateTime, // Musi być DateTime
                HttpMethod.Get
            );

            return signedUrl;
        }
    }
}
