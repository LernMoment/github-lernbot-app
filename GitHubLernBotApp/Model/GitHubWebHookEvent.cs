using GitHubLernBotApp.Extensions;
using Octokit.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GitHubLernBotApp.Model
{
    class GitHubWebHookEvent
    {
        private readonly string _webHookSecret;

        public GitHubWebHookEvent(string webHookSecret, HttpRequestHeaders headers, HttpContent content)
        {
            _webHookSecret = webHookSecret;
            EventName = headers.GetValues("X-GitHub-Event").FirstOrDefault();
            DeliveryId = headers.GetValues("X-GitHub-Delivery").FirstOrDefault();
            Signature = headers.GetValues("X-Hub-Signature").FirstOrDefault();
            Payload = content;
        }

        public string EventName { get; }

        public string DeliveryId { get; }

        public string Signature { get; }

        public HttpContent Payload { get; }

        public bool ContainsExpectedData() => !string.IsNullOrEmpty(EventName) && !string.IsNullOrEmpty(DeliveryId) && !string.IsNullOrEmpty(Signature);

        public async Task<bool> IsMessageAuthenticated()
        {
            if (Payload == null)
                return false;
            if (string.IsNullOrEmpty(Signature))
                return false;

            var payloadBytes = await Payload.ReadAsByteArrayAsync();

            var key = Encoding.ASCII.GetBytes(_webHookSecret);
            var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(payloadBytes);
            var actualSignature = "sha1=" + hash.ToHexString();
            
            return SecureEquals(actualSignature, Signature);
        }

        public async Task<T> DeserializePayload<T>()
        {
            string json = await Payload.ReadAsStringAsync();
            var serializer = new SimpleJsonSerializer();
            return serializer.Deserialize<T>(json);
        }

        // Constant-time comparison
        private bool SecureEquals(string a, string b)
        {
            int len = Math.Min(a.Length, b.Length);
            bool equals = a.Length == b.Length;
            for (int i = 0; i < len; i++)
            {
                equals &= (a[i] == b[i]);
            }

            return equals;
        }
    }
}
