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
            request.Method = method.ToString();

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
