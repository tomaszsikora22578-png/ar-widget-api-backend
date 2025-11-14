// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using Google.Apis.Auth.OAuth2; // To jest potrzebne dla GoogleCredential
using Google.Cloud.Storage.V1.Signing; // Ta linijka jest KLUCZOWA (i problematyczna)

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private readonly UrlSigner _urlSigner; // Zmieniamy z powrotem na UrlSigner
        private const string BucketName = "ar-models-dla-klientow"; 

        public GcsService()
        {
            // GoogleCredential.GetApplicationDefault() jest asynchroniczne,
            // ale działa synchronicznie w Cloud Run do pobrania poświadczeń.
            var credential = GoogleCredential.GetApplicationDefault();
            
            // To jest instancja, która prawidłowo obsługuje podpisywanie w Cloud Run
            _urlSigner = UrlSigner.FromCredential(credential); 
        }

        public string GenerateSignedUrl(string objectName)
        {
            var expiration = DateTimeOffset.UtcNow.AddMinutes(5); 

            // Używamy metody instancyjnej Sign z UrlSigner
            string signedUrl = _urlSigner.Sign(
                bucketName: BucketName,
                objectName: objectName,
                expiration: expiration,
                method: HttpMethod.Get
            );
            
            return signedUrl;
        }
    }
}
