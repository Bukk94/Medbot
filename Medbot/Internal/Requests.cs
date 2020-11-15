using Medbot.Commands;
using Medbot.Enums;
using Medbot.LoggingNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Medbot.Internal
{
    public class Requests
    {
        /// <summary>
        /// Gets a Client ID from oAuth token
        /// </summary>
        /// <param name="token">oAuth token</param>
        /// <returns>Returns string client ID</returns>
        public async static Task<string> GetClientID(string token)
        {
            if (String.IsNullOrEmpty(token))
            {
                Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), "Token was null or empty");
                return null;
            }

            if (token.Contains("oauth:"))
                token = token.Replace("oauth:", "");

            string url = String.Format("https://api.twitch.tv/kraken?oauth_token={0}", token);
            var serverResponse = JsonConvert.DeserializeObject(TwitchJsonRequest(url, RequestType.GET));
            dynamic parsedJson = JObject.Parse(serverResponse.ToString());
            return parsedJson.token.client_id.Value;
        }

        /// <summary>
        /// Gets JSON response of the given url
        /// </summary>
        /// <param name="url">URL to request JSON file</param>
        /// <param name="method">POST/GET HTTP method</param>
        /// <returns>Request results</returns>
        public static string TwitchJsonRequest(string url, RequestType method, string payload = null)
        {
            if (string.IsNullOrEmpty(Login.ClientID))
                throw new ArgumentException("Client ID is missing!");
            
            if (string.IsNullOrEmpty(Login.BotOauth))
                throw new ArgumentException("OAuth is missing!");

            if (method == RequestType.POST && string.IsNullOrEmpty(payload))
                Console.WriteLine("WARNING: POST request was sent without payload!");

            var request = WebRequest.CreateHttp(url);
            request.Headers["Client-ID"] = Login.ClientID;
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers["Authorization"] = $"Bearer {Login.BotOauth}";
            request.Method = nameof(method);

            if (payload != null)
                using (var writer = new StreamWriter(request.GetRequestStreamAsync().GetAwaiter().GetResult()))
                    writer.Write(payload);

            try
            {
                var response = (HttpWebResponse)request.GetResponse();

                using (var reader = new StreamReader(response.GetResponseStream()))
                    return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                return null;
            }
        }
    }
}
