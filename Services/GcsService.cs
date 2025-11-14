// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
// USUŃ: using Google.Apis.Auth.OAuth2; 
// USUŃ: using Google.Cloud.Storage.V1.Signing; // Tego i tak nie widać

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private readonly StorageClient _storageClient; // Teraz używamy instancji klienta
        private const string BucketName = "ar-models-dla-klientow"; 
        
        // Ta metoda nie działa w Cloud Run bez klucza JSON:
        // private readonly GoogleCredential _credential; 

        public GcsService()
        {
            // StorageClient.Create() automatycznie pobiera poświadczenia konta serwisowego
            // Cloud Run, co jest KLUCZOWE dla działania tej metody.
            _storageClient = StorageClient.Create(); 
        }

        public string GenerateSignedUrl(string objectName)
        {
            var expiration = DateTime.UtcNow.AddMinutes(5); 

            // Używamy metody instancyjnej StorageClient
            string signedUrl = _storageClient.CreateSignedUrl(
                bucket: BucketName,
                objectName: objectName,
                expiration: expiration,
                method: HttpMethod.Get
            );
            
            return signedUrl;
        }
    }
}
