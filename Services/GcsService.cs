// GcsService.cs
using System.Net.Http;
using System;
using System.IO; 
using System.Text;
using System.Security.Cryptography; // KLUCZOWE: Wymagane do podpisu RSA
using Google.Apis.Auth.OAuth2; 
using Google.Apis.Auth.OAuth2.ServiceAccount; // Wymagane dla ServiceAccountCredential
using Newtonsoft.Json; // Wymagane do odczytu klucza JSON

namespace ArWidgetApi.Services 
{
    public class GcsService
    {
        private const string BucketName = "ar-models-dla-klientow";
        private readonly string _serviceAccountEmail;
        private readonly RSA _rsaSigner; // Obiekt do podpisu

        public GcsService()
        {
            // --- 1. ŁADOWANIE KLUCZA PRYWATNEGO I ID Z PLIKU JSON ---
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
            
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
            {
                throw new InvalidOperationException("Klucz GCS nie został poprawnie zamontowany. Sprawdź, czy GCS_PRIVATE_KEY_PATH jest ustawiony na ścieżkę pliku JSON.");
            }
            
            var json = File.ReadAllText(keyPath);
            
            // Używamy narzędzi Google tylko do ekstrakcji klucza i ID z JSON
            var credentialInitializer = ServiceAccountCredential.FromServiceAccountData(json).CreateInitializer();
            
            _serviceAccountEmail = credentialInitializer.User;
            
            // --- 2. KONWERSJA KLUCZA PEM NA OBIEKT .NET RSA ---
            var keyInPemFormat = credentialInitializer.Key;
            
            _rsaSigner = RSA.Create();
            try
            {
                _rsaSigner.ImportFromPem(keyInPemFormat);
            }
            catch (Exception ex)
            {
                // Błąd wczytywania klucza (np. nieprawidłowy format PEM)
                throw new InvalidOperationException($"Błąd ładowania klucza RSA: {ex.Message}");
            }
        }

        public string GenerateSignedUrl(string objectName)
        {
            TimeSpan duration = TimeSpan.FromMinutes(5);
            DateTime expiryTime = DateTime.UtcNow.Add(duration);
            
            // Format daty dla nagłówków i podpisu
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            // --- 1. TWORZENIE ŁAŃCUCHA DO PODPISU (V4) ---
            string urlPath = $"/{BucketName}/{objectName}";
            
            // Canonical Request (muszą być puste linie)
            string canonicalRequest = $"GET\n{urlPath}\n\n\nhost:storage.googleapis.com\nx-goog-date:{timestamp}\n\nhost;x-goog-date\n";
            
            // String To Sign
            string stringToSign = $"GOOG4-RSA-SHA256\n{timestamp}\n/storage/goog4_request\n{SHA256Hash(canonicalRequest)}";

            // --- 2. PODPISANIE ŁAŃCUCHA KLUCZEM PRYWATNYM ---
            byte[] signatureBytes;
            using (var sha256 = SHA256.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(stringToSign);
                // Podpisanie danych za pomocą obiektu RSA (używa klucza prywatnego)
                signatureBytes = _rsaSigner.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            string base64Signature = Convert.ToBase64String(signatureBytes);
            // Konwersja na Base64 i kodowanie URL
            string hexSignature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLowerInvariant();

            // --- 3. SKŁADANIE KOŃCOWEGO URLA ---
            string signedUrl = $"https://storage.googleapis.com{urlPath}" +
                               $"?X-Goog-Signature={hexSignature}" +
                               $"&X-Goog-Algorithm=GOOG4-RSA-SHA256" +
                               $"&X-Goog-Credential={Uri.EscapeDataString(_serviceAccountEmail)}%2F{dateStamp}%2Fauto%2Fstorage%2Fgoog4_request" +
                               $"&X-Goog-Date={timestamp}" +
                               $"&X-Goog-Expires={(long)duration.TotalSeconds}" +
                               $"&X-Goog-SignedHeaders=host%3Bx-goog-date";
                               
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
