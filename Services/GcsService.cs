using Google.Cloud.Storage.V1;
using System.Net.Http;
using System;

namespace ArWidgetApi.Services // Zmień namespace na swój
{
    public class GcsService
    {
        private readonly StorageClient _storageClient;
        // !!! ZMIEŃ NA NAZWĘ SWOJEGO BUCKETA !!!
        private const string BucketName = "ar-models-dla-klientow"; 

        public GcsService()
        {
            // Klient automatycznie pobierze poświadczenia z konta serwisowego Cloud Run
            _storageClient = StorageClient.Create();
        }

        /// <summary>
        /// Generuje tymczasowy, podpisany adres URL dla obiektu w GCS (GLB lub USDZ).
        /// Używamy tego dla prywatnych (płatnych) zasobów.
        /// </summary>
        /// <param name="objectName">Pełna ścieżka obiektu GCS (np. 'clients/KLIENT_A_ID/fotel.usdz')</param>
        /// <returns>Tymczasowy URL do pobrania modelu.</returns>
        public string GenerateSignedUrl(string objectName)
        {
            // Czas ważności linku: 5 minut.
            var expiration = DateTimeOffset.UtcNow.AddMinutes(5).DateTime;
            
            // Konieczne jest użycie HttpMethod.Get
            var url = _storageClient.CreateSignedUrl(
                bucketName: BucketName,
                objectName: objectName,
                expiration: expiration,
                method: HttpMethod.Get
            );
            
            return url;
        }
    }
}
