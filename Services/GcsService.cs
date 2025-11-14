// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using System.Threading.Tasks; 
using System.IO; 
using Google.Apis.Auth.OAuth2; // ZWRÓCONY! Wymagany do wczytania klucza
using Google.Apis.Auth.OAuth2.Responses; // Dodatkowe (na wszelki wypadek)

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow";
        private readonly string _serviceAccountId; 
        private readonly string _privateKey; // Będziemy trzymać klucz prywatny

        public GcsService()
        {
            // POBRANIE ŚCIEŻKI DO KLUCZA JSON Z Cloud Run (np. /etc/secrets/gcs_key.json)
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");

            if (string.IsNullOrEmpty(keyPath))
            {
                // Fallback dla lokalnego developmentu lub kompilacji
                _serviceAccountId = "849496305543-compute@developer.gserviceaccount.com";
                _privateKey = ""; // Klucz będzie pusty, jeśli nie ma pliku
            }
            else
            {
                // Wczytanie klucza z pliku JSON
                string json = File.ReadAllText(keyPath);
                var token = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResponse>(json);
                
                // Używamy ServiceAccountCredential, aby wyciągnąć klucz i ID
                var credential = GoogleCredential.FromFile(keyPath).UnderlyingCredential as ServiceAccountCredential;

                if (credential == null)
                {
                    throw new InvalidOperationException("Nie udało się załadować ServiceAccountCredential. Sprawdź plik JSON i uprawnienia.");
                }
                
                _serviceAccountId = credential.Id; // Automatycznie pobiera ID
                _privateKey = credential.PrivateKey; // Automatycznie pobiera klucz
            }
        }

        public string GenerateSignedUrl(string objectName)
        {
            TimeSpan duration = TimeSpan.FromMinutes(5); 

            if (string.IsNullOrEmpty(_privateKey))
            {
                // Wersja bez podpisu, jeśli klucz nie został załadowany (np. w kompilacji)
                // W Cloud Run to powinno się ZAWSZE nie wydarzyć.
                return $"https://storage.googleapis.com/{BucketName}/{objectName}";
            }

            // UŻYCIE NAJBARDZIEJ STABILNEJ, STATYCZNEJ METODY CreateV4SignedUrl
            // Sygnatura metody: bucketName, objectName, duration, method, serviceAccountEmail, privateKey
            string signedUrl = Google.Cloud.Storage.V1.UrlSigner.CreateV4SignedUrl(
                BucketName,
                objectName,
                duration,
                HttpMethod.Get,
                _serviceAccountId, // Argument 5 (string - Service Account ID)
                _privateKey        // Argument 6 (string - Private Key)
            );
            
            return signedUrl;
        }
    }
}
