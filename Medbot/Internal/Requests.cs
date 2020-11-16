using Medbot.Enums;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Medbot.Internal
{
    public class Requests
    {
        /// <summary>
        /// Gets JSON response of the given url
        /// </summary>
        /// <param name="url">URL to request JSON file</param>
        /// <param name="method">POST/GET HTTP method</param>
        /// <returns>Request results</returns>
        public static async Task<string> TwitchJsonRequestAsync(string url, RequestType method, string payload = null)
        {
            if (string.IsNullOrEmpty(Login.ClientID))
                throw new ArgumentException("Client ID is missing!");
            
            if (string.IsNullOrEmpty(Login.BotOauth))
                throw new ArgumentException("OAuth is missing!");

            if (method == RequestType.POST && string.IsNullOrEmpty(payload))
                Console.WriteLine("WARNING: POST request was sent without payload!");

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Client-ID", Login.ClientID);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Login.BotOauth);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            if (method == RequestType.POST && payload != null)
            {
                var payloadContent = new StringContent(payload);
                payloadContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = client.PostAsync(url, payloadContent).Result;
            }
            else
            {
                response = await client.GetAsync(new Uri(url));
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            return content;
        }
    }
}
