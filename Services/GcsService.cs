// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using Google.Apis.Auth.OAuth2; 
// USUŃ: using Google.Cloud.Storage.V1.Signing; 

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow"; 

        // KONTENERY CLOUD RUN UŻYWAJĄ INNEGO MECHANIZMU DO PODPISYWANIA URLI
        // UŻYJEMY METODY CreateSignedUrl, KTÓRA JEST DOSTĘPNA W PODSTAWOWEJ BIBLIOTECE.

        // Do tej metody POTRZEBUJEMY JAWNIE USTAWIĆ POŚWIADCZENIA.
        // W Cloud Run, GoogleCredential.GetApplicationDefault() powinien zadziałać.
        private readonly GoogleCredential _credential; 

        public GcsService()
        {
            // To jest asynchroniczne, ale w konstruktorze musi być synchroniczne.
            // Użycie GetApplicationDefault() jest poprawne w kontekście Cloud Run.
            _credential = GoogleCredential.GetApplicationDefault(); 
        }

        public string GenerateSignedUrl(string objectName)
        {
            // Domyślny czas wygaśnięcia
            var expiration = DateTime.UtcNow.AddMinutes(5); 

            // Używamy metody statycznej, która jest bardziej stabilna
            string signedUrl = UrlSigner.CreateSignedUrl(
                bucket: BucketName,
                objectName: objectName,
                expiration: expiration,
                method: HttpMethod.Get,
                credential: _credential // Przekazujemy poświadczenia
                // Opcjonalnie: ContentType = "model/gltf-binary" (dla glb)
            );

            return signedUrl;
        }
    }
}
