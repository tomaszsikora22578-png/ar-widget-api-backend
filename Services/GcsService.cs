using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using Google.Apis.Auth.OAuth2;

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private readonly GoogleCredential _credential; 
        private const string BucketName = "ar-models-dla-klientow"; 

        public GcsService()
        {
            // To powinno być dostępne, ponieważ to podstawowa metoda autoryzacji
            _credential = GoogleCredential.GetApplicationDefault();
        }

        public string GenerateSignedUrl(string objectName)
        {
            var expiration = DateTime.UtcNow.AddMinutes(5); 

            // Używamy metody statycznej CreateV4SignedUrl.
            // W Cloud Run, to najczęściej wymaga przekazania ID konta serwisowego
            // lub użycia dedykowanego klucza. Zaczniemy od użycia poświadczeń.

            string signedUrl = Google.Cloud.Storage.V1.UrlSigner.CreateV4SignedUrl(
                bucketName: BucketName,
                objectName: objectName,
                duration: TimeSpan.FromMinutes(5), // Długość ważności
                method: HttpMethod.Get,
                credential: _credential // Używamy naszego poświadczenia
            );

            return signedUrl;
        }
    }
}
