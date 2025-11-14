using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
            string keyPath = Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY_PATH");
            if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
                throw new InvalidOperationException("Klucz GCS nie został poprawnie zamontowany.");

            var json = File.ReadAllText(keyPath);
            var keyData = JsonConvert.DeserializeObject<ServiceAccountKey>(json);

            _serviceAccountEmail = keyData.ClientEmail;

            _rsa = RSA.Create();
            _rsa.ImportFromPem(keyData.PrivateKey);
        }

        public string GenerateSignedUrl(string objectName)
        {
            TimeSpan expires = TimeSpan.FromMinutes(5);

            string method = "GET";
            string host = "storage.googleapis.com";

            string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            string region = "auto"; // GCS tego i tak ignoruje, ale musi być w standardzie
            string credentialScope = $"{dateStamp}/{region}/storage/goog4_request";

            string canonicalUri = $"/{BucketName}/{objectName}";
            string canonicalQueryString = "";

            // ------------------------
            // CANONICAL HEADERS
            // ------------------------
            string canonicalHeaders =
                $"host:{host}\n" +
                $"x-goog-date:{timestamp}\n";

            string signedHeaders = "host;x-goog-date";

            // ------------------------
            // CANONICAL REQUEST
            // ------------------------
            string canonicalRequest =
                $"{method}\n" +
                $"{canonicalUri}\n" +
                $"{canonicalQueryString}\n" +
                $"{canonicalHeaders}\n" +
                $"{signedHeaders}\n" +
                $"{SHA256Hex("")}";   // empty payload for GET

            string canonicalRequestHash = SHA256Hex(canonicalRequest);

            // ------------------------
            // STRING TO SIGN
            // ------------------------
            string stringToSign =
                "GOOG4-RSA-SHA256\n" +
                $"{timestamp}\n" +
                $"{credentialScope}\n" +
                $"{canonicalRequestHash}";

            // ------------------------
            // SIGNATURE
            // ------------------------
            byte[] signatureBytes = _rsa.SignData(
                Encoding.UTF8.GetBytes(stringToSign),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            string signatureHex = BitConverter
                .ToString(signatureBytes)
                .Replace("-", "")
                .ToLowerInvariant();

            // ------------------------
            // FINAL SIGNED URL
            // ------------------------
            string finalUrl =
                $"https://{host}{canonicalUri}" +
                $"?X-Goog-Algorithm=GOOG4-RSA-SHA256" +
                $"&X-Goog-Credential={Uri.EscapeDataString(_serviceAccountEmail) }%2F{credentialScope}" +
                $"&X-Goog-Date={timestamp}" +
                $"&X-Goog-Expires={(int)expires.TotalSeconds}" +
                $"&X-Goog-SignedHeaders={Uri.EscapeDataString(signedHeaders)}" +
                $"&X-Goog-Signature={signatureHex}";

            return finalUrl;
        }

        private static string SHA256Hex(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
