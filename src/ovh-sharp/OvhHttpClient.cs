using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OvhSharp
{
    /// <summary>
    /// Class implementing the IOvhHttpClient
    /// </summary>
    public class OvhHttpClient : IOvhHttpClient
    {
        private const string HTTP_GET = "GET";
        private const string HTTP_POST = "POST";
        private const string HTTP_DELETE = "DELETE";

        private const string HEADER_X_OVH_TIMESTAMP = "X-Ovh-Timestamp";
        private const string HEADER_X_OVH_SIGNATURE = "X-Ovh-Signature";

        private readonly string applicationSecret;
        private readonly string applicationKey;
        private readonly string consumerKey;

        private readonly HttpClient httpClient;

        /// <summary>
        /// Creates a new instance of the class
        /// </summary>
        /// <param name="applicationSecret">The Application Secret used to connect to the OVH API</param>
        /// <param name="applicationKey">The Application Key used to connect to the OVH API</param>
        /// <param name="consumerKey">The Consumer Secret used to connect to the OVH API</param>
        public OvhHttpClient(string applicationSecret, string applicationKey, string consumerKey)
        {
            this.applicationSecret = applicationSecret;
            this.applicationKey = applicationKey;
            this.consumerKey = consumerKey;

            this.httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Ovh-Application", this.applicationKey);
            httpClient.DefaultRequestHeaders.Add("X-Ovh-Consumer", this.consumerKey);
        }

        private static long GetTimestamp()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        private string GetRequestSignature(string httpMethod, string url, string body, long timestamp)
        {
            string preHash = $"{applicationSecret}+{consumerKey}+{httpMethod}+{url}+{body}+{timestamp}";
            string signature = "$1$";

            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] buffer = Encoding.UTF8.GetBytes(preHash);
                var hash = sha1.ComputeHash(buffer);
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                signature += sb.ToString();
            }

            return signature;
        }

        /// <inheritdoc />
        public JToken Get(string url)
        {
            this.SignRequest(HTTP_GET, url, string.Empty);

            Task<string> stringTask = httpClient.GetStringAsync(url);
            string json = stringTask.Result;
            JToken token = JToken.Parse(json);
            return token;
        }

        /// <inheritdoc />
        public JToken Post(string requestUri, string body)
        {
            this.SignRequest(HTTP_POST, requestUri, body);

            Task<HttpResponseMessage> responseTask = httpClient.PostAsync(requestUri, new StringContent(body, Encoding.UTF8, "application/json"));
            HttpResponseMessage response = responseTask.Result;

            if (false == response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Request failed. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Response Content: {response.Content}");
            }

            try
            {
                var token = JToken.Parse(response.Content.ToString());
                return token;
            }
            catch (JsonReaderException)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public JToken Delete(string uri)
        {
            this.SignRequest(HTTP_DELETE, uri, string.Empty);
            var responseTask = this.httpClient.DeleteAsync(uri);
            var response = responseTask.Result;

            if (false == response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Request failed. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Response Content: {response.Content}");
            }

            try
            {
                string json = response.Content.ToString();
                var token = JToken.Parse(json);
                return token;
            }
            catch (JsonReaderException)
            {
                return null;
            }
        }

        private void SignRequest(string httpMethod, string url, string body)
        {
            long timestamp = GetTimestamp();
            string requestSignature = this.GetRequestSignature(httpMethod, url, body, timestamp);

            var headers = this.httpClient.DefaultRequestHeaders;
            headers.Remove(HEADER_X_OVH_TIMESTAMP);
            headers.Remove(HEADER_X_OVH_SIGNATURE);
            headers.Add(HEADER_X_OVH_TIMESTAMP, timestamp.ToString());
            headers.Add(HEADER_X_OVH_SIGNATURE, requestSignature);
        }
    }
}
