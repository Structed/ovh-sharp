using Newtonsoft.Json.Linq;

namespace OvhSharp
{
    /// <summary>
    /// Interface for an OVH HTTP Client
    /// </summary>
    public interface IOvhHttpClient
    {
        /// <summary>
        /// Send a HTTP GET request to the given <paramref name="requestUri"/>
        /// </summary>
        /// <param name="requestUri">The URi to request</param>
        /// <returns>A JToken object representing the JSON object returned or null</returns>
        JToken Get(string requestUri);

        /// <summary>
        /// Send a HTTP POST request to the given <paramref name="requestUri"/>,
        /// with a <paramref name="body"/>
        /// </summary>
        /// <param name="requestUri">The URI to request</param>
        /// <param name="body">The response body to send</param>
        /// <returns>A JToken object representing the JSON object returned or null</returns>
        JToken Post(string requestUri, string body);

        /// <summary>
        /// Send a HTTP DELETE request to the given <paramref name="requestUri"/>
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns>A JToken object representing the JSON object returned or null</returns>
        JToken Delete(string requestUri);
    }
}