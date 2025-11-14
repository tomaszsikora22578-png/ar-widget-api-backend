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
            var keyData = JsonConvert.DeserializeObject<ServiceAccountKey>(json)
                          ?? throw new InvalidOperationException("Nie udało się wczytać danych klucza GCS.");

            _serviceAccountEmail = keyData.ClientEmail ?? throw new InvalidOperationException("Brak client_email w pliku klucza.");
            _rsa = RSA.Create();
            _rsa.ImportFromPem(keyData.PrivateKey);
        }

        // *****************************************************
        // GET - pobieranie pliku
        // *****************************************************
        public string GenerateSignedUrl(string objectName, int expiresSeconds = 300)
        {
            string method = "GET";
            string host = "storage.googleapis.com";

            string timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

            string region = "auto";
            string credentialScope = $"{dateStamp}/{region}/storage/goog4_request";

            string canonicalUri = $"/{BucketName}/{objectName}";

            // canonical headers i signed headers
            string canonicalHeaders = $"host:{host}\n";
            string signedHeaders = "host";

            // -------------------------------
            // CanonicalQueryString (alfabetycznie posortowane)
            // -------------------------------
            string[] queryParams =
            {
                $"X-Goog-Algorithm=GOOG4-RSA-SHA256",
                $"X-Goog-Credential={_serviceAccountEmail}/{credentialScope}",
                $"X-Goog-Date={timestamp}",
                $"X-Goog-Expires={expiresSeconds}",
                $"X-Goog-SignedHeaders={signedHeaders}"
            };
            Array.Sort(queryParams, StringComparer.Ordinal);
            string canonicalQueryString = string.Join("&", queryParams);

            // canonical request
            string canonicalRequest =
                $"{method}\n" +
                $"{canonicalUri}\n" +
                $"{canonicalQueryString}\n" +
                $"{canonicalHeaders}\n" +
                $"{signedHeaders}\n" +
                $"UNSIGNED-PAYLOAD";

            string canonicalRequestHash = SHA256Hex(canonicalRequest);

            // string to sign
            string stringToSign =
                "GOOG4-RSA-SHA256\n" +
                $"{timestamp}\n" +
                $"{credentialScope}\n" +
                $"{canonicalRequestHash}";

            // podpis RSA
            byte[] signature = _rsa.SignData(
                Encoding.UTF8.GetBytes(stringToSign),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            string signatureHex = BitConverter.ToString(signature).Replace("-", "").ToLowerInvariant();

            // final URL - escape tylko wartości
            string[] urlParams =
            {
                $"X-Goog-Algorithm=GOOG4-RSA-SHA256",
                $"X-Goog-Credential={Uri.EscapeDataString(_serviceAccountEmail + "/" + credentialScope)}",
                $"X-Goog-Date={timestamp}",
                $"X-Goog-Expires={expiresSeconds}",
                $"X-Goog-SignedHeaders={signedHeaders}"
            };
            string finalUrlQuery = string.Join("&", urlParams);

            return $"https://{host}{canonicalUri}?{finalUrlQuery}&X-Goog-Signature={signatureHex}";
        }

        // *****************************************************
        // SHA256 Hex helper
        // *****************************************************
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
