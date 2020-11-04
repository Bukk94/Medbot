using Medbot.Commands;
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
            var serverResponse = JsonConvert.DeserializeObject(await TwitchJsonRequest(url, "GET"));
            dynamic parsedJson = JObject.Parse(serverResponse.ToString());
            return parsedJson.token.client_id.Value;
        }

        /// <summary>
        /// Gets JSON response of the given url
        /// </summary>
        /// <param name="url">URL to request JSON file</param>
        /// <param name="method">POST/GET HTTP method</param>
        /// <returns></returns>
        public async static Task<string> TwitchJsonRequest(string url, string method)
        {
            var request = WebRequest.CreateHttp(url);
            request.Method = method;
            request.ContentType = "application/json";

            try
            {
                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                var data = await reader.ReadToEndAsync();
                return data;
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                return null;
            }
        }
    }
}
