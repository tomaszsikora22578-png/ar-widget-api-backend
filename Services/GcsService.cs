using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
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
        private readonly RSA _rsa;

        public GcsService()
        {
            string json = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_JSON");
            if (!string.IsNullOrEmpty(json))
            {
                json = json.Replace("\\n", "\n");
            }
            else
            {
                string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
                if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
                    throw new InvalidOperationException("Klucz GCS nie został poprawnie zamontowany.");
                json = File.ReadAllText(keyPath);
            }

            var keyData = JsonConvert.DeserializeObject<ServiceAccountKey>(json)
                          ?? throw new InvalidOperationException("Nie udało się wczytać danych klucza GCS.");

            _serviceAccountEmail = keyData.ClientEmail ?? throw new InvalidOperationException("Brak client_email w pliku klucza.");
            _rsa = RSA.Create();
            _rsa.ImportFromPem(keyData.PrivateKey);
        }

        public string GenerateSignedUrl(string objectName, int expiresSeconds = 300)
        {
            string method = "GET";
            string host = "storage.googleapis.com";

            string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            string region = "auto";
            string credentialScope = $"{dateStamp}/{region}/storage/goog4_request";

            string canonicalUri = $"/{BucketName}/{objectName}";

            // Canonical headers
            string canonicalHeaders = $"host:{host}\n";
            string signedHeaders = "host";

            // Canonical query string
            var queryParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                {"X-Goog-Algorithm", "GOOG4-RSA-SHA256"},
                {"X-Goog-Credential", $"{_serviceAccountEmail}/{credentialScope}"},
                {"X-Goog-Date", timestamp},
                {"X-Goog-Expires", expiresSeconds.ToString()},
                {"X-Goog-SignedHeaders", signedHeaders}
            };

            string canonicalQueryString = string.Join("&",
                queryParams.Select(kvp =>
                    $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

            // Canonical request
            string canonicalRequest = string.Join("\n",
                method,
                canonicalUri,
                canonicalQueryString,
                canonicalHeaders,
                signedHeaders,
                "UNSIGNED-PAYLOAD");

            string canonicalRequestHash = SHA256Hex(canonicalRequest);

            // String to sign
            string stringToSign = string.Join("\n",
                "GOOG4-RSA-SHA256",
                timestamp,
                credentialScope,
                canonicalRequestHash);

            // Sign
            byte[] signatureBytes = _rsa.SignData(
                Encoding.UTF8.GetBytes(stringToSign),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            string signatureHex = BitConverter.ToString(signatureBytes).Replace("-", "").ToLowerInvariant();

            // Final URL
            string finalUrlQuery = string.Join("&",
                queryParams.Select(kvp =>
                    $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

            return $"https://{host}{canonicalUri}?{finalUrlQuery}&X-Goog-Signature={signatureHex}";
        }

        private static string SHA256Hex(string input)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
