// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using Google.Apis.Auth.OAuth2; 
// USUŃ: using Google.Cloud.Storage.V1.Signing; // UNIKAMY TEGO BŁĘDU

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow"; 

        public GcsService()
        {
            // Nie potrzebujemy inicjalizacji w konstruktorze
            // jeśli używamy statycznej metody UrlSigner.Sign, 
            // która przyjmuje poświadczenia jako argument.
        }

        public string GenerateSignedUrl(string objectName)
        {
            var expiration = DateTimeOffset.UtcNow.AddMinutes(5);
            
            // POBRANIE POŚWIADCZEŃ (jest to najstabilniejsze w Cloud Run)
            var credential = GoogleCredential.GetApplicationDefault();

            // UŻYCIE STATYCZNEJ METODY UrlSigner.Sign Z JAWNYM POŚWIADCZENIEM
            // Wymaga użycia klasy UrlSigner.
            // Sprawdź dokumentację: ta metoda często działa, nawet gdy inne zawodzą.
            string signedUrl = Google.Cloud.Storage.V1.UrlSigner.Sign(
                bucketName: BucketName,
                objectName: objectName,
                expiration: expiration,
                method: HttpMethod.Get,
                credential: credential // Przekazujemy poświadczenia
            );
            
            return signedUrl;
        }
    }
}
