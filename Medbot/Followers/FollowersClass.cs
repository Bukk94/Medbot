using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Medbot.LoggingNS;
using Medbot.Commands;
using Medbot.Internal;

namespace Medbot.Followers {
    /// <summary>
    /// Order of followers list - Ascended or Descended
    /// </summary>
    public enum ListDirection { asc, desc }
    public static class FollowersClass {

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
                clientID = await Requests.GetClientID(Login.BotOauth);

            string url = String.Format(@"https://api.twitch.tv/kraken/channels/{0}/follows?client_id={1}&limit={2}&direction={3}", 
                                        channel.ToLower(), clientID, limit, dir);


            return JsonConvert.DeserializeObject<FollowersInfo>(await Requests.TwitchJsonRequest(url, "GET"));
        }

        /// <summary>
        /// Gets single follower's info
        /// </summary>
        /// <param name="followerUsername">Name of follower to look at</param>
        /// <param name="clientID">Bot's client ID, if null it will be calculated from oAuth</param>
        /// <returns>Return Follower object containing all info about follower</returns>
        public async static Task<Follower> GetFollowerFollowsInfo(string channel, string followerUsername, string clientID = null) {
            if (channel.Equals(followerUsername, StringComparison.InvariantCultureIgnoreCase)) // return if owner is trying to get info about himself
                return null;

            if (clientID == null)
                clientID = await Requests.GetClientID(Login.BotOauth);

            // https://api.twitch.tv/kraken/users/<user ID>/follows/channels/<channel id>?client_id=<client id>
            string url = String.Format(@"https://api.twitch.tv/kraken/users/{0}/follows/channels/{1}?client_id={2}", followerUsername.ToLower(), channel.ToLower(), clientID);

            return JsonConvert.DeserializeObject<Follower>(await Requests.TwitchJsonRequest(url, "GET"));
        }
    }
}
