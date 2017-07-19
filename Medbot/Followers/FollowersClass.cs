using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Medbot.LoggingNS;
using Medbot.Commands;

namespace Medbot.Followers {
    /// <summary>
    /// Order of followers list - Ascended or Descended
    /// </summary>
    enum ListDirection { asc, desc }
    internal static class FollowersClass {

        /// <summary>
        /// Gets a newest follower
        /// </summary>
        /// <param name="channel">Channel name to search</param>
        /// <param name="clientID">Bot's client ID, if null it will calculated from oauth</param>
        /// <returns>Follower object of the lastest follower on channel</returns>
        public async static Task<Follow> GetNewestFollower(string channel, string clientID) {
            FollowersInfo info = await GetFollowersInfo(channel, 1, clientID, ListDirection.desc);
            if(info == null)
                return null;

            return info.FollowersList.Count > 0 ? info.FollowersList[0] : null;
        }

        /// <summary>
        /// Gets number of followers from given channel
        /// </summary>
        /// <param name="channel">Name of channel</param>
        /// <param name="limit">Number of follower to return, max 100</param>
        /// <param name="clientID">Bot's client ID, if null it will be calculated from oAuth</param>
        /// <param name="dir">Direction of list - asc/desc</param>
        /// <returns>Returns list of followers</returns>
        public async static Task<List<Follow>> GetFollowers(string channel, int limit, string clientID = null, ListDirection dir = ListDirection.desc) {
            var data = await GetFollowersInfo(channel, limit, clientID, dir);
            return data.FollowersList;
        }

        /// <summary>
        /// Gets a FollowersInfo structure containing information about channel's followers and list of followers
        /// </summary>
        /// <param name="channel">Name of channel</param>
        /// <param name="limit">Number of follower to return, max 100</param>
        /// <param name="clientID">Bot's client ID, if null it will be calculated from oAuth</param>
        /// <param name="dir">Direction of list - asc/desc</param>
        /// <returns>Returns FollowersInfo structure containing information about channel's followers and list of followers</returns>
        public async static Task<FollowersInfo> GetFollowersInfo(string channel, int limit, string clientID = null, ListDirection dir = ListDirection.desc) {
            //string url = @"https://api.twitch.tv/kraken/channels/bukk94/follows?client_id=q6batx0epp608isickayubi39itsckt&limit=5&direction=desc";
            channel = channel.ToLower();

            if (limit > 100) // Can't return more than 100 values
                limit = 100;

            if(clientID == null)
                clientID = await GetClientID(Login.BotOauth);

            string url = String.Format(@"https://api.twitch.tv/kraken/channels/{0}/follows?client_id={1}&limit={2}&direction={3}", 
                                        channel.ToLower(), clientID, limit, dir);


            return JsonConvert.DeserializeObject<FollowersInfo>(await TwitchJsonRequest(url, "GET"));
        }

        /// <summary>
        /// Gets a Client ID from oAuth token
        /// </summary>
        /// <param name="token">oAuth token</param>
        /// <returns>Returns string client ID</returns>
        public async static Task<string> GetClientID(string token) {
            if (String.IsNullOrEmpty(token)) {
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
        private async static Task<string> TwitchJsonRequest(string url, string method) {
            var request = WebRequest.CreateHttp(url);
            request.Method = method;
            request.ContentType = "application/json";

            try {
                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                var data = await reader.ReadToEndAsync();
                return data;
            } catch (Exception ex) {
                Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                return null;
            }
        }
    }
}
