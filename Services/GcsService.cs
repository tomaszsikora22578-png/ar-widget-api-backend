// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using Google.Apis.Auth.OAuth2; 
using System.Threading.Tasks; // Dodaj, jeśli brakuje

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow"; 

        // Konstruktor jest teraz prosty, bez inicjalizacji.
        public GcsService()
        {
        }

        public string GenerateSignedUrl(string objectName)
        {
            // 1. Zmieńmy wygasanie na TimeSpan
            // (np. 5 minut od teraz)
            TimeSpan duration = TimeSpan.FromMinutes(5); 
            
            // 2. Pobierz poświadczenia
            var credential = GoogleCredential.GetApplicationDefault();

            // 3. Używamy statycznej metody CreateV4SignedUrl,
            // która wymaga TimeSpan i GoogleCredential jako argumentów
            // Argumenty pozycyjne: bucketName, objectName, duration, method, credential
            string signedUrl = Google.Cloud.Storage.V1.UrlSigner.CreateV4SignedUrl(
                BucketName,             // Argument 1 (string)
                objectName,             // Argument 2 (string)
                duration,               // Argument 3 (TimeSpan - POPRAWIONE)
                HttpMethod.Get,         // Argument 4 (HttpMethod)
                credential              // Argument 5 (GoogleCredential - POPRAWIONE)
            );
            
            return signedUrl;
        }
    }
}
