using Google.Cloud.Storage.V1; // Zawiera klasę StorageClient
using System.Net.Http;
using System;
using Google.Apis.Auth.OAuth2; // Zawiera GoogleCredential
using Google.Cloud.Storage.V1.Signing; // TO ZAWIERA KLASĘ UrlSigner

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        // ... Pola read-only ...
        private const string BucketName = "ar-models-dla-klientow"; 
        
        // POTRZEBUJEMY TEGO DLA GENEROWANIA URL
        private readonly UrlSigner _urlSigner; // NOWE POLE

        public GcsService()
        {
            // UrlSigner automatycznie używa poświadczeń Cloud Run
            // W środowisku Google Cloud, to działa automatycznie.
            var credential = GoogleCredential.GetApplicationDefault();
            _urlSigner = UrlSigner.FromCredential(credential); // NOWA INICJALIZACJA
        }

        public string GenerateSignedUrl(string objectName)
        {
            var expiration = DateTimeOffset.UtcNow.AddMinutes(5);
            
            // NOWA IMPLEMENTACJA Z UŻYCIEM UrlSigner
            var url = _urlSigner.Sign(
                BucketName,
                objectName,
                expiration,
                HttpMethod.Get
            );
            
            return url;
        }
    }
}
