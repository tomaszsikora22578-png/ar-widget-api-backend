// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using System.IO; 
using Google.Apis.Auth.OAuth2; 
using Google.Apis.Auth.OAuth2.ServiceAccount; // Dodane dla ServiceAccountCredential

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow";
        private readonly ServiceAccountCredential _signingCredential; // Używamy konkretnego typu

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
                generalCredential = GoogleCredential.FromFile(keyPath);
            }

            // KLUCZOWY KROK: JAWNE POBRANIE ServiceAccountCredential
            // Rzutowanie na konkretny typ, który jest wymagany do podpisywania.
            _signingCredential = generalCredential.UnderlyingCredential as ServiceAccountCredential;
            
            if (_signingCredential == null)
            {
                // To jest krytyczny błąd, jeśli używamy domyślnego klucza Cloud Run
                throw new InvalidOperationException("Nie udało się uzyskać ServiceAccountCredential do podpisywania URLi. Upewnij się, że klucz JSON jest poprawnie zamontowany.");
            }
        }

        public string GenerateSignedUrl(string objectName)
        {
            TimeSpan duration = TimeSpan.FromMinutes(5); 
            
            // MUSIMY UŻYĆ METODY UrlSigner.Sign, KTÓRA PRZYJMUJE ServiceAccountCredential
            // Sygnatura, która musi działać, to ta z pięcioma argumentami, 
            // gdzie ostatni argument to ServiceAccountCredential (nie GoogleCredential)
            string signedUrl = Google.Cloud.Storage.V1.UrlSigner.Sign(
                BucketName,
                objectName,
                duration,
                HttpMethod.Get,
                _signingCredential // Przekazanie konkretnego typu ServiceAccountCredential
            );
            
            return signedUrl;
        }
    }
}
