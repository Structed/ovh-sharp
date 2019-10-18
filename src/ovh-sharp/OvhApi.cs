using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OvhSharp
{
    /// <summary>
    /// Request class for the OVH API
    /// </summary>
    public class OvhApi
    {
        private readonly OvhHttpClient ovhHttpClient;

        /// <summary>
        /// Creates an new instance of the Request class.
        ///
        /// See https://api.ovh.com/g934.first_step_with_api on how to get your credentials
        /// </summary>
        /// <param name="applicationSecret">The Application Secret, see https://api.ovh.com/g934.first_step_with_api on how to get it</param>
        /// <param name="applicationKey">The Application Secret, see https://api.ovh.com/g934.first_step_with_api on how to get it</param>
        /// <param name="consumerKey">Consumer Key or "authentication token". See https://api.ovh.com/g934.first_step_with_api#creating_identifiers_requesting_an_authentication_token_from_ovh</param>
        public OvhApi(string applicationSecret, string applicationKey, string consumerKey)
        {
            this.ovhHttpClient = new OvhHttpClient(applicationSecret, applicationKey, consumerKey);
        }

        /// <summary>
        /// Retrieve's a Domain Zone's detail
        /// </summary>
        /// <param name="zoneName">The name of the zone, <example>example.com</example></param>
        /// <returns>JToken representation of the response</returns>
        public JToken GetDomainZone(string zoneName)
        {
            var token = this.ovhHttpClient.Get($"https://eu.api.ovh.com/1.0/domain/zone/{zoneName}");
            return token;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="zoneName">The name of the zone, <example>example.com</example></param>
        /// <param name="fieldType">The DNS record field type <example>CNAME</example></param>
        /// <param name="subDomain">The subdomain the record applies to. If it is not applicable, leave it empty.</param>
        /// <returns></returns>
        /// <example>
        /// To retrieve all records for the subdomain mail of example.com (mail.example.com):
        /// <code>
        /// <![CDATA[
        /// IEnumerable<int> records =  GetDomainZoneRecords("example.com", "CNAME", "mail")
        /// ]]>
        /// </code>
        /// </example>
        public IEnumerable<int> GetDomainZoneRecords(string zoneName, string fieldType = "", string subDomain = "")
        {
            string query = $"https://eu.api.ovh.com/1.0/domain/zone/{zoneName}/record";

            var queryParameters = new Dictionary<string, string> {{"fieldType", fieldType}, {"subDomain", subDomain}};
            string parameters = this.AssembleQueryParameters(queryParameters);

            if (string.IsNullOrEmpty(parameters) == false)
            {
                query += $"?{parameters}";
            }

            var token = this.ovhHttpClient.Get(query);
            IEnumerable<int> result = token.ToObject<List<int>>();

            return result;
        }

        /// <summary>
        /// Retrieves the details of a Domain Zone Record
        /// </summary>
        /// <param name="zoneName">The name of the zone, <example>example.com</example></param>
        /// <param name="id">The ID of the resource. <see cref="GetDomainZoneRecords"/></param>
        /// <returns></returns>
        public JToken GetDomainZoneRecordDetails(string zoneName, int id)
        {
            string query = $"https://eu.api.ovh.com/1.0/domain/zone/{zoneName}/record/{id}";
            JToken result = this.ovhHttpClient.Get(query);
            return result;
        }

        /// <summary>
        /// Creates a new DNS record.
        ///
        /// Don't forget to refresh the zone afterwards!
        /// <see cref="RefreshDomainZone(string)"/>
        /// </summary>
        /// <param name="zoneName">The name of the zone, <example>example.com</example></param>
        /// <param name="fieldType">The DNS record field type <example>CNAME</example></param>
        /// <param name="target">Where the record points to. Ends with a dot character (.)</param>
        /// <param name="subDomain">The subdomain the record applies to. If it is not applicable, leave it empty.</param>
        /// <param name="ttl">The Time To Live, in seconds, of the record. DNS replication will not happen *before* this given interval</param>
        public JToken PostDomainZoneRecord(string zoneName, string fieldType, string target, string subDomain, long? ttl)
        {
            if (string.IsNullOrWhiteSpace(fieldType) || string.IsNullOrWhiteSpace(target))
            {
                throw new ArgumentException($"The parameters {nameof(fieldType)} and {nameof(target)} must not be null, empty or consist only of whitespace!");
            }

            string query = $"https://eu.api.ovh.com/1.0/domain/zone/{zoneName}/record";

            JObject bodyObject = new JObject();
            bodyObject.Add("fieldType", fieldType);
            bodyObject.Add("target", target);

            if (false == string.IsNullOrWhiteSpace(subDomain))
            {
                bodyObject.Add("subDomain", subDomain);
            }

            if (ttl != null && ttl >= 0)
            {
                bodyObject.Add("ttl", ttl);
            }

            JToken result = this.ovhHttpClient.Post(query, bodyObject.ToString());

            return result;
        }

        /// <summary>
        /// Deletes a DNS record
        /// </summary>
        /// <param name="zoneName">The name of the zone, <example>example.com</example></param>
        /// <param name="id">The numeric ID of the record. Use <see cref="GetDomainZoneRecords"/> to get the record IDs</param>
        public void DeleteDomainZoneRecord(string zoneName, int id)
        {
            string query = $"https://eu.api.ovh.com/1.0/domain/zone/{zoneName}/record/{id}";
            this.ovhHttpClient.Delete(query);
        }

        /// <summary>
        /// Deletes DNS Zone Records
        /// </summary>
        /// <param name="zoneName">The name of the zone, <example>example.com</example></param>
        /// <param name="ids">The IDs to delete</param>
        public void DeleteDomainZoneRecord(string zoneName, IEnumerable<int> ids)
        {
            // TODO: Delete individual records asynchronously
            foreach (int id in ids)
            {
                this.DeleteDomainZoneRecord(zoneName, id);
            }
        }

        public void DeleteDomainZoneRecord(string zoneName, IEnumerable<JToken> recordDetails)
        {
            IEnumerable<int> ids = recordDetails.Select(x => int.Parse(x["id"].ToString()));
            this.DeleteDomainZoneRecord(zoneName, ids);
        }

        /// <summary>
        /// Refreshes the Domain Zone.
        /// Required to call after making changes to the Zone
        /// </summary>
        /// <param name="zoneName">The name of the zone, <example>example.com</example></param>
        public void RefreshDomainZone(string zoneName)
        {
            this.ovhHttpClient.Post($"https://eu.api.ovh.com/1.0/domain/zone/{zoneName}/refresh", string.Empty);
        }

        private string AssembleQueryParameters(Dictionary<string, string> queryParameters)
        {
            if (queryParameters.Count < 1)
            {
                throw new ArgumentException($"{nameof(queryParameters)} must not be empty");
            }

            string parameterString = "";
            foreach (KeyValuePair<string, string> parameter in queryParameters)
            {
                if (string.IsNullOrEmpty(parameter.Value))
                {
                    continue;
                }

                string key = System.Net.WebUtility.UrlEncode(parameter.Key);
                string value = System.Net.WebUtility.UrlEncode(parameter.Value);

                parameterString += $"&{key}={value}";
            }

            parameterString = parameterString.TrimStart('&');

            return parameterString;
        }
    }
}
