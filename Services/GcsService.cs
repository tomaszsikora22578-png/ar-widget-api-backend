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
            // --- 1. RĘCZNE ŁADOWANIE KLUCZA PRYWATNEGO I ID Z PLIKU JSON ---
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
            
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                // BŁĄD ZAMONTOWANIA (JUŻ GO ROZWIĄZALIŚMY)
                throw new InvalidOperationException("Klucz GCS nie został poprawnie zamontowany.");
            }
            
            var json = File.ReadAllText(keyPath);
            
            var keyData = JsonConvert.DeserializeObject<ServiceAccountKey>(json);
            
            _serviceAccountEmail = keyData.ClientEmail;
            
            // --- 2. KONWERSJA KLUCZA PEM NA OBIEKT .NET RSA ---
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

            // --- 1. TWORZENIE ŁAŃCUCHA DO PODPISU (V4) ---
            string urlPath = $"/{BucketName}/{objectName}";
            
            string signedHeadersList = "host;x-goog-date";

            // Normalizacja wartości dla Kanonicznych Nagłówków (Trymowanie i małe litery)
            string hostValue = "storage.googleapis.com".Trim().ToLowerInvariant(); 
            string dateValue = timestamp.Trim().ToLowerInvariant(); 

            // Kanoniczne Żądanie (Canonical Request)
            // Użycie StringBuilder dla precyzyjnej kontroli nad separatorami \n
            var sb = new StringBuilder();
            
            // 1. HTTP Method
            sb.Append("GET").Append("\n");
            
            // 2. Canonical URI
            sb.Append(urlPath).Append("\n");
            
            // 3. Canonical Query String (pusta)
            sb.Append("\n"); 
            
            // 4. Canonical Headers (host:value\nx-goog-date:value\n)
            // TO JEST KRYTYCZNA SEKCJA, KTÓRA PRAWIE ZAWSZE POWODUJE BŁĄD HOST/DATE
            sb.Append("host:").Append(hostValue).Append("\n"); 
            sb.Append("x-goog-date:").Append(dateValue).Append("\n"); 
            
            // 5. Hash of Payload (pusta linia, bo GET)
            sb.Append("\n"); 
            
            // 6. Signed Headers (host;x-goog-date)
            sb.Append(signedHeadersList); 
            
            string canonicalRequest = sb.ToString();

            // String To Sign
            string stringToSign = $"GOOG4-RSA-SHA256\n{timestamp}\n/storage/goog4_request\n{SHA256Hash(canonicalRequest)}";

            // --- 2. PODPISANIE ŁAŃCUCHA KLUCZEM PRYWATNYM ---
            byte[] signatureBytes;
            using (var sha256 = SHA256.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(stringToSign);
                signatureBytes = _rsaSigner.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            string hexSignature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLowerInvariant();

            // --- 3. SKŁADANIE KOŃCOWEGO URLA ---
            string signedUrl = $"https://storage.googleapis.com{urlPath}" +
                                $"?X-Goog-Signature={hexSignature}" +
                                $"&X-Goog-Algorithm=GOOG4-RSA-SHA256" +
                                $"&X-Goog-Credential={Uri.EscapeDataString(_serviceAccountEmail)}%2F{dateStamp}%2Fauto%2Fstorage%2Fgoog4_request" +
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
