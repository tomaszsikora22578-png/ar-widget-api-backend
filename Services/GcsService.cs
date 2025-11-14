using System.Net.Http;
using System;
using System.IO; 
using System.Text;
using System.Security.Cryptography; 
using Newtonsoft.Json; 

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private class ServiceAccountKey
        {
            [JsonProperty("private_key")]
            public string PrivateKey { get; set; }
            
            [JsonProperty("client_email")]
            public string ClientEmail { get; set; }
        }

        private const string BucketName = "ar-models-dla-klientow";
        private readonly string _serviceAccountEmail;
        private readonly RSA _rsaSigner; 

        public GcsService()
        {
            // ... (KONSTRUKTOR POZOSTAJE BEZ ZMIAN)
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                throw new InvalidOperationException("Klucz GCS nie został poprawnie zamontowany.");
            }
            var json = File.ReadAllText(keyPath);
            var keyData = JsonConvert.DeserializeObject<ServiceAccountKey>(json);
            _serviceAccountEmail = keyData.ClientEmail;
            var keyInPemFormat = keyData.PrivateKey; 
            _rsaSigner = RSA.Create();
            try
            {
                _rsaSigner.ImportFromPem(keyInPemFormat);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd ładowania klucza RSA: {ex.Message}");
            }
        }

        public string GenerateSignedUrl(string objectName)
        {
            TimeSpan duration = TimeSpan.FromMinutes(5);
            
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"); 
            string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");
            string region = "auto"; // Dla Cloud Run jest to region globalny/auto

            // --- 1. TWORZENIE ŁAŃCUCHA DO PODPISU (V4) ---
            string urlPath = $"/{BucketName}/{objectName}";
            
            string signedHeadersList = "host;x-goog-date";

            // Normalizacja wartości
            string hostValue = "storage.googleapis.com".Trim().ToLowerInvariant(); 
            string dateValue = timestamp.Trim().ToLowerInvariant(); 

            // Kanoniczne Żądanie (Canonical Request) - Bez zmian od ostatniej, rygorystycznej wersji
            var sb = new StringBuilder();
            sb.Append("GET").Append("\n");
            sb.Append(urlPath).Append("\n");
            sb.Append("\n"); 
            sb.Append("host:").Append(hostValue).Append("\n"); 
            sb.Append("x-goog-date:").Append(dateValue).Append("\n"); 
            sb.Append("\n"); 
            sb.Append(signedHeadersList); 
            
            string canonicalRequest = sb.ToString();

            // String To Sign - KRYTYCZNA POPRAWKA PEŁNEGO SCOPE
            string stringToSign = string.Concat(
                "GOOG4-RSA-SHA256\n",
                timestamp, "\n",
                dateStamp, $"/storage/goog4_request\n", // Używamy formatu GCS bez regionu, ale z datą
                // Poprawka: W standardzie GCS Signed URLs, jest to często:
                // {dateStamp}/{region}/storage/goog4_request. Używamy auto.
                // Użyjemy formatu z Credential, który wydaje się działać:
                dateStamp, "/", region, "/storage/goog4_request\n",
                SHA256Hash(canonicalRequest)
            );
            
            // Wracamy do poprzedniego, czystszego StringToSign, który działał w Twoim URL-u:
            string stringToSignFinal = $"GOOG4-RSA-SHA256\n{timestamp}\n{dateStamp}/{region}/storage/goog4_request\n{SHA256Hash(canonicalRequest)}";


            // --- 2. PODPISANIE ŁAŃCUCHA KLUCZEM PRYWATNYM ---
            byte[] signatureBytes;
            using (var sha256 = SHA256.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(stringToSignFinal);
                signatureBytes = _rsaSigner.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            string hexSignature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLowerInvariant();

            // --- 3. SKŁADANIE KOŃCOWEGO URLA ---
            string signedUrl = $"https://storage.googleapis.com{urlPath}" +
                                $"?X-Goog-Signature={hexSignature}" +
                                $"&X-Goog-Algorithm=GOOG4-RSA-SHA256" +
                                $"&X-Goog-Credential={Uri.EscapeDataString(_serviceAccountEmail)}%2F{dateStamp}%2F{region}%2Fstorage%2Fgoog4_request" +
                                $"&X-Goog-Date={timestamp}" +
                                $"&X-Goog-Expires={(long)duration.TotalSeconds}" +
                                $"&X-Goog-SignedHeaders={Uri.EscapeDataString(signedHeadersList.Replace(";", "%3B"))}"; 
                                
            return signedUrl;
        }

        private string SHA256Hash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
