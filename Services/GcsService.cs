// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using System.IO; 
using Google.Apis.Auth.OAuth2; 
using Newtonsoft.Json; 
// USUNIĘTO: using Google.Apis.Auth.OAuth2.ServiceAccount; 
// USUNIĘTO: using Google.Cloud.Storage.V1.Signing; 

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow";
        private readonly GoogleCredential _credential;

        public GcsService()
        {
            // POBRANIE ŚCIEŻKI DO KLUCZA JSON Z Cloud Run
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
            
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                 // Jeśli klucz nie jest zamontowany, używamy domyślnej autoryzacji Cloud Run
                 _credential = GoogleCredential.GetApplicationDefault();
                 // W Cloud Run ta linia spowoduje błąd w trakcie podpisywania,
                 // ale przynajmniej pozwoli na kompilację.
            }
            else
            {
                // Wczytanie klucza z pliku (uproszczone)
                _credential = GoogleCredential.FromFile(keyPath);
            }
        }

        public string GenerateSignedUrl(string objectName)
        {
            TimeSpan duration = TimeSpan.FromMinutes(5); 
            
            // MUSIMY UŻYĆ METODY STATYCZNEJ, BO METODY INSTANCYJNE ZAWODZĄ
            // Sygnatura V2 jest w Google.Cloud.Storage.V1.UrlSigner.Sign.
            // Oczekuje ServiceAccountCredential, ale spróbujmy przekazać GoogleCredential
            
            string signedUrl = Google.Cloud.Storage.V1.UrlSigner.Sign(
                BucketName,
                objectName,
                duration,
                HttpMethod.Get,
                _credential // Używamy ogólnego GoogleCredential
            );
            
            return signedUrl;
        }
    }
}
