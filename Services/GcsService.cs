// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
using System.IO; 
using Google.Apis.Auth.OAuth2; 
// USUNIĘTO: using Google.Apis.Auth.OAuth2.ServiceAccount; 

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow";
        private readonly StorageClient _storageClient;

        public GcsService()
        {
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
            GoogleCredential credential;
            
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                 // Użycie domyślnej autoryzacji dla Cloud Run
                 credential = GoogleCredential.GetApplicationDefault();
            }
            else
            {
                // Wczytanie klucza z pliku (najczystszy sposób)
                credential = GoogleCredential.FromFile(keyPath);
            }
            
            // TWORZYMY INSTANCJĘ KLIENTA
            _storageClient = StorageClient.Create(credential);
        }

        public string GenerateSignedUrl(string objectName)
        {
            // Zmieniamy sygnaturę, aby użyć przeciążenia akceptującego StorageClient
            
            // JEST TO JEDYNA STABILNA, STATYCZNA METODA, KTÓRA MUSI ISTNIEĆ:
            // UrlSigner.Sign(StorageClient, bucket, object, duration, method)
            string signedUrl = Google.Cloud.Storage.V1.UrlSigner.Sign(
                _storageClient, // Przekazujemy klienta z wczytanymi poświadczeniami
                BucketName,
                objectName,
                TimeSpan.FromMinutes(5),
                HttpMethod.Get
            );
            
            return signedUrl;
        }
    }
}
