using System;
using System.IO;
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

        private readonly string _bucketName = "ar-models-dla-klientow";
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

        public string GenerateSignedUrl(string objectName, int expiresMinutes = 5)
        {
            // request basics
            string host = "storage.googleapis.com";
            string httpMethod = "GET";

            // timestamps
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
            string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            // credential scope
            string region = "auto";
            string credentialScope = $"{dateStamp}/{region}/storage/goog4_request";

            // canonical URI
            string canonicalUri = $"/{_bucketName}/{objectName}";

            // canonical headers (only host!)
            string canonicalHeaders = $"host:{host}\n";

            // signed headers (only host!)
            string signedHeaders = "host";

            // URL params (canonical query string)
            string credential = Uri.EscapeDataString($"{_serviceAccountEmail}/{credentialScope}");

            string canonicalQueryString =
                $"X-Goog-Algorithm=GOOG4-RSA-SHA256" +
                $"&X-Goog-Credential={credential}" +
                $"&X-Goog-Date={timestamp}" +
                $"&X-Goog-Expires={expiresMinutes * 60}" +
                $"&X-Goog-SignedHeaders={signedHeaders}";

            // payload hash for GET
            string payloadHash = SHA256Hex("");

            // canonical request
            string canonicalRequest =
                $"{httpMethod}\n" +
                $"{canonicalUri}\n" +
                $"{canonicalQueryString}\n" +
                $"{canonicalHeaders}\n" +
                $"{signedHeaders}\n" +
                $"{payloadHash}";

            string canonicalRequestHash = SHA256Hex(canonicalRequest);

            // string to sign
            string stringToSign =
                "GOOG4-RSA-SHA256\n" +
                $"{timestamp}\n" +
                $"{credentialScope}\n" +
                $"{canonicalRequestHash}";

            // sign
            byte[] signatureBytes = _rsa.SignData(
                Encoding.UTF8.GetBytes(stringToSign),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            string signatureHex = BitConverter.ToString(signatureBytes)
                .Replace("-", "")
                .ToLowerInvariant();

            // final URL
            string finalUrl =
                $"https://{host}{canonicalUri}?" +
                $"{canonicalQueryString}" +
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
