// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using System.IO; 
using Google.Apis.Auth.OAuth2; // Zostawiamy
using Google.Cloud.Storage.V1.Signing; // Dodajemy to ZNOWU, bo musimy wymusić, że jest
using Google.Apis.Auth.OAuth2.ServiceAccount; // Dodajemy to
using Google.Cloud.Storage.V1.Implementation; // Dodajemy to

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow";
        private readonly ServiceAccountCredential.Initializer _credentialInitializer;
        private readonly string _serviceAccountId;

        public GcsService()
        {
            // POBRANIE ŚCIEŻKI DO KLUCZA JSON Z Cloud Run
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
            
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                // W przypadku, gdy plik nie jest dostępny, używamy domyślnego klucza
                throw new InvalidOperationException("Klucz prywatny GCS nie został poprawnie zamontowany w Cloud Run. Wymagany plik JSON: /etc/secrets/gcs_key.json.");
            }
            
            // 1. ODCZYT I INICJALIZACJA Z PLIKU
            var json = File.ReadAllText(keyPath);
            _credentialInitializer = ServiceAccountCredential.FromServiceAccountData(json).CreateInitializer();
            
            // 2. POBRANIE ID KONTA SERWISOWEGO
            _serviceAccountId = _credentialInitializer.User;
        }

        public string GenerateSignedUrl(string objectName)
        {
            TimeSpan duration = TimeSpan.FromMinutes(5); 

            // 3. TWORZENIE PODPISU ZA POMOCĄ INICJALIZATORA
            // Używamy metody, która przyjmuje ServiceAccountCredential i duration.
            
            // Najpierw tworzymy instancję StorageClient z klucza,
            // ponieważ CreateSignedUrl istnieje tylko na instancji
            var credential = new ServiceAccountCredential(_credentialInitializer);
            var client = StorageClient.Create(credential);

            // UŻYCIE INSTANCYJNEJ METODY, która jest w najnowszych bibliotekach
            string signedUrl = client.CreateSignedUrl(
                BucketName,
                objectName,
                DateTime.UtcNow.Add(duration), // Musi być DateTime, nie TimeSpan
                HttpMethod.Get,
                new CreateSignedUrlOptions() 
                {
                    // Ustawienie wersji V4
                    SigningVersion = SigningVersion.V4,
                    // Wymagany Service Account ID
                    // Krok ten często jest pomijany, ale jest kluczowy w Cloud Run
                    ServiceAccountEmail = _serviceAccountId 
                });

            return signedUrl;
        }
    }
}
