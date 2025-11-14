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
    
    // POBRANIE POŚWIADCZEŃ (W Cloud Run to jest stabilne)
    var credential = GoogleCredential.GetApplicationDefault();

    // UŻYCIE STATYCZNEJ METODY UrlSigner.Sign Z ARGUMENTAMI POZYCYJNYMI
    // Usuwamy jawne nazwy parametrów (`bucketName:`, `objectName:` itp.)
    string signedUrl = Google.Cloud.Storage.V1.UrlSigner.Sign(
        BucketName,             // 1. Nazwa Bucketa (bucketName)
        objectName,             // 2. Nazwa Obiektu (objectName)
        expiration,             // 3. Wygasanie (expiration)
        HttpMethod.Get,         // 4. Metoda HTTP (method)
        credential              // 5. Poświadczenia (credential)
    );
    
    return signedUrl;
}
    }
}
