// GcsService.cs
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
        // Klasa pomocnicza do deserializacji klucza JSON - TERAZ ZAGNIEDZONA W KLASIE GcsService
        private class ServiceAccountKey
        {
            // Można usunąć to pole, ponieważ nie jest używane w logice
            // [JsonProperty("private_key_id")]
            // public string PrivateKeyId { get; set; }
            
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
                throw new InvalidOperationException("Klucz GCS nie został poprawnie zamontowany.");
            }
            
            var json = File.ReadAllText(keyPath);
            
            // UŻYCIE Newtonsoft.Json do ręcznego wczytania danych
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

        // Metoda GenerateSignedUrl pozostaje bez zmian
public string GenerateSignedUrl(string objectName)
{
    TimeSpan duration = TimeSpan.FromMinutes(5);
    
    // Upewniamy się, że format daty jest poprawny
    string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"); 
    string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

    // --- 1. TWORZENIE ŁAŃCUCHA DO PODPISU (V4) ---
    string urlPath = $"/{BucketName}/{objectName}";
    
    // Kluczowe elementy do poprawnego podpisu:
    string hostHeader = "host:storage.googleapis.com\n";
    string dateHeader = $"x-goog-date:{timestamp}\n";
    string signedHeadersList = "host;x-goog-date";

    // 
    // Kanoniczne Żądanie (Canonical Request)
    //
    string canonicalRequest = string.Concat(
        "GET\n", 
        urlPath, "\n",    // URI + \n
        "\n",              // Query String (pusta) + \n
        hostHeader,        // host:storage.googleapis.com\n
        dateHeader,        // x-goog-date:...\n
        "\n",              // Payload Hash (pusta linia) + \n
        signedHeadersList  // host;x-goog-date (BEZ KOŃCOWEGO \n!)
    );
    
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
        
        // Funkcja pomocnicza do hashowania SHA256 (Hash of the Canonical Request)
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
