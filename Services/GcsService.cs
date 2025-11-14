// GcsService.cs - Ostateczna próba z V2
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using System.IO; 
using Google.Apis.Auth.OAuth2; 
using Google.Apis.Auth.OAuth2.ServiceAccount; // Wymagane do ServiceAccountCredential
using Google.Cloud.Storage.V1.Signing; // Przywracamy, bo jest potrzebne do SigningVersion

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow";
        private readonly string _serviceAccountEmail;
        private readonly string _privateKey;

        public GcsService()
        {
            // Ładowanie klucza z pliku JSON (Niezmienione - jest poprawne)
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
            
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                 throw new InvalidOperationException("Plik klucza GCS nie został poprawnie zamontowany pod ścieżką GCS_PRIVATE_KEY_PATH.");
            }
            
            var json = File.ReadAllText(keyPath);
            var credentialInitializer = ServiceAccountCredential.FromServiceAccountData(json).CreateInitializer();
            
            _serviceAccountEmail = credentialInitializer.User;
            _privateKey = credentialInitializer.Key;
        }

        public string GenerateSignedUrl(string objectName)
        {
            TimeSpan duration = TimeSpan.FromMinutes(5); 
            
            // UŻYCIE NAJBARDZIEJ STABILNEJ METODY V2 SIGNED URL
            // Sygnatura: (bucket, object, duration, method, serviceAccountEmail, privateKey, signingVersion)
            // Wymaga powrotu do prostszej metody UrlSigner.Sign.
            string signedUrl = Google.Cloud.Storage.V1.UrlSigner.Sign(
                BucketName,
                objectName,
                duration,
                HttpMethod.Get,
                _serviceAccountEmail, 
                _privateKey,           
                // JAWNIE OKREŚLAMY WERSJĘ JAKO V2
                SigningVersion.V2 
            );
            
            return signedUrl;
        }
    }
}
