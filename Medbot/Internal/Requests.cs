using Medbot.Enums;
using Medbot.Internal.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Medbot.Internal
{
    public class Requests
    {
        internal static LoginDetails LoginDetails { get; set; }

        /// <summary>
        /// Gets JSON response of the given url
        /// </summary>
        /// <param name="url">URL to request JSON file</param>
        /// <param name="method">POST/GET HTTP method</param>
        /// <returns>Request results</returns>
        public static async Task<string> TwitchJsonRequestAsync(string url, RequestType method, string payload = null)
        {
            if (string.IsNullOrEmpty(LoginDetails.ClientID))
                throw new ArgumentException("Client ID is missing!");
            
            if (string.IsNullOrEmpty(LoginDetails.BotOAuth))
                throw new ArgumentException("OAuth is missing!");
            
            if (method == RequestType.POST && string.IsNullOrEmpty(payload))
                Console.WriteLine("WARNING: POST request was sent without payload!");

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Client-ID", LoginDetails.ClientID);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LoginDetails.BotOAuth);
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

        internal async static Task<long> GetUserId(string username)
        {
            var data = await TwitchJsonRequestAsync($"https://api.twitch.tv/helix/users?login={username}", RequestType.GET);
            return GetJsonData(data).First.Value<long>("id");
        }

        internal static JToken GetJsonData(string json)
        {
            var parsedJson = JObject.Parse(json);
            return parsedJson["data"];
        }
    }
}
