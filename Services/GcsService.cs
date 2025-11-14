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
            // expiration
            TimeSpan expires = TimeSpan.FromMinutes(5);

            // request basics
            string method = "GET";
            string host = "storage.googleapis.com";

            // timestamps - IMPORTANT: do not change case/format after this point
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ"); // e.g. 20251114T150000Z
            string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");       // e.g. 20251114

            string region = "auto";
            string credentialScope = $"{dateStamp}/{region}/storage/goog4_request";

            string canonicalUri = $"/{BucketName}/{objectName}";

            // canonical headers (names must be lowercase)
            string canonicalHeaders =
                $"host:{host}\n" +
                $"x-goog-date:{timestamp}\n";

            // signed headers list (lowercase, semicolon separated)
            string signedHeaders = "host;x-goog-date";

            // ------------------------
            // canonical query string (must be included in canonical request, keys sorted)
            // ------------------------
            // We url-encode credential value (email/credentialScope) using EscapeDataString
            string credentialValueEscaped = Uri.EscapeDataString(_serviceAccountEmail + "/" + credentialScope);

            // Note: X-Goog-SignedHeaders in canonical query string must be the literal "host;x-goog-date"
            // and its value must NOT be additionally escaped beyond what Uri.EscapeDataString does for other fields.
            // For safety, we'll include signedHeaders unescaped in canonicalQueryString, but when composing final URL
            // we'll use Uri.EscapeDataString for the parameter values where appropriate.
            string canonicalQueryString =
                $"X-Goog-Algorithm=GOOG4-RSA-SHA256" +
                $"&X-Goog-Credential={credentialValueEscaped}" +
                $"&X-Goog-Date={timestamp}" +
                $"&X-Goog-Expires={(int)expires.TotalSeconds}" +
                $"&X-Goog-SignedHeaders={signedHeaders}";

            // ------------------------
            // canonical request
            // ------------------------
            // payload hash for GET is SHA256 of empty string
            string payloadHash = SHA256Hex("");

            string canonicalRequest =
                $"{method}\n" +
                $"{canonicalUri}\n" +
                $"{canonicalQueryString}\n" +
                $"{canonicalHeaders}\n" +
                $"{signedHeaders}\n" +
                $"{payloadHash}";

            string canonicalRequestHash = SHA256Hex(canonicalRequest);

            // ------------------------
            // string to sign
            // ------------------------
            string stringToSign =
                "GOOG4-RSA-SHA256\n" +
                $"{timestamp}\n" +
                $"{credentialScope}\n" +
                $"{canonicalRequestHash}";

            // ------------------------
            // signature (sign stringToSign using RSA private key, SHA256, PKCS#1 v1.5)
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
            // final URL: canonicalQueryString (already contains all except signature) + signature param
            // make sure to escape signed headers in final URL
            // ------------------------
            string finalUrl =
                $"https://{host}{canonicalUri}?" +
                canonicalQueryString +
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
