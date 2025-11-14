// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using System.IO; 
using Google.Apis.Auth.OAuth2; 
using Google.Apis.Auth.OAuth2.ServiceAccount; // Wymagane do ServiceAccountCredential

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow";
        private readonly ServiceAccountCredential _signingCredential; 

        public GcsService()
        {
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
            GoogleCredential generalCredential;
            
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                 generalCredential = GoogleCredential.GetApplicationDefault();
            }
            else
            {
                // Uproszczone wczytywanie klucza z pliku
                generalCredential = GoogleCredential.FromFile(keyPath);
            }

            // KLUCZOWY KROK: Rzutowanie na ServiceAccountCredential musi pozostać,
            // ponieważ jest to typ wewnętrzny wymagany do podpisu.
            _signingCredential = generalCredential.UnderlyingCredential as ServiceAccountCredential;
            
            if (_signingCredential == null)
            {
                throw new InvalidOperationException("Nie udało się uzyskać ServiceAccountCredential do podpisywania URLi. Sprawdź, czy konto serwisowe jest prawidłowo skonfigurowane.");
            }
        }

        public string GenerateSignedUrl(string objectName)
        {
            TimeSpan duration = TimeSpan.FromMinutes(5); 
            
            // Używamy V2 Signed URL, jedynej sygnatury, która może zadziałać 
            // z przekazaniem ServiceAccountCredential jako piątego argumentu.
            string signedUrl = Google.Cloud.Storage.V1.UrlSigner.Sign(
                BucketName,
                objectName,
                duration,
                HttpMethod.Get,
                _signingCredential 
            );
            
            return signedUrl;
        }
    }
}
