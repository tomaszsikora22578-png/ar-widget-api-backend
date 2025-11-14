// GcsService.cs
using Google.Cloud.Storage.V1; 
using System.Net.Http;
using System;
// USUŃ: using Google.Apis.Auth.OAuth2; 
using System.Threading.Tasks; 
using System.IO; // Już niepotrzebne

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow"; 
        
        // ID konta serwisowego używanego do podpisywania
        private readonly string _serviceAccountId; 

        public GcsService()
        {
            // POBRANIE ID KONTA SERWISOWEGO CLOUD RUN
            // W Cloud Run to jest standardowa zmienna środowiskowa,
            // ale jeśli jest pusta, użyjemy domyślnej konwencji.
            _serviceAccountId = Environment.GetEnvironmentVariable("K_SERVICE_ACCOUNT") 
                                ?? "849496305543-compute@developer.gserviceaccount.com";
            
            // W CZASIE KOMPILACJI TA ZMIENNA BĘDZIE PUSTA,
            // dlatego użyjemy tylko metody statycznej, która nie wymaga instancji.
        }

        public string GenerateSignedUrl(string objectName)
        {
            TimeSpan duration = TimeSpan.FromMinutes(5); 

            // UŻYCIE STATYCZNEJ METODY UrlSigner.Sign z JAWNYM ID KONTA SERWISOWEGO
            // To jest metoda, która ma mniej przeciążeń i działa stabilniej.
            string signedUrl = Google.Cloud.Storage.V1.UrlSigner.Sign(
                BucketName,
                objectName,
                duration,
                HttpMethod.Get,
                _serviceAccountId // Przekazujemy ID konta serwisowego
            );
            
            return signedUrl;
        }
    }
}
