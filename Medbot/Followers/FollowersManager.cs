using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Medbot.Internal;
using Medbot.Enums;
using Medbot.Users;

namespace Medbot.Followers
{
    /// <summary>
    /// Order of followers list - Ascending or Descending
    /// </summary>
    public enum ListDirection { asc, desc }
    public static class FollowersManager
    {
        /// <summary>
        /// Gets a newest follower
        /// </summary>
        /// <param name="channel">Channel name to search</param>
        /// <param name="clientID">Bot's client ID, if null it will calculated from oauth</param>
        /// <returns>Follower object of the lastest follower on channel</returns>
        public async static Task<Follow> GetNewestFollower(string channel, string clientID)
        {
            FollowersInfo info = await GetFollowersInfo(channel, 1, clientID, ListDirection.desc);
            if (info == null)
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
        public async static Task<List<Follow>> GetFollowers(string channel, int limit, string clientID = null, ListDirection dir = ListDirection.desc)
        {
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
        public async static Task<FollowersInfo> GetFollowersInfo(string channel, int limit, string clientID = null, ListDirection dir = ListDirection.desc)
        {
            // TODO: Check v5 kraken API endpoint is gone. Replace it with new helix endpoint with client-id header
            // https://api.twitch.tv/helix/users/follows?to_id=24395849&first=1

            //string url = @"https://api.twitch.tv/kraken/channels/bukk94/follows?client_id=q6batx0epp608isickayubi39itsckt&limit=5&direction=desc";
            channel = channel.ToLower();

            if (limit > 100) // Can't return more than 100 values
                limit = 100;

            string url = String.Format(@"https://api.twitch.tv/kraken/channels/{0}/follows?client_id={1}&limit={2}&direction={3}",
                                        channel.ToLower(), clientID, limit, dir);


            return JsonConvert.DeserializeObject<FollowersInfo>(await Requests.TwitchJsonRequestAsync(url, RequestType.GET));
        }

        public async static Task<DateTime?> GetFollowDate(long channelId, User follower)
        {
            if (follower.ID <= 0)
                await follower.UpdateUserId();

            if (channelId == follower.ID) // return if owner is trying to get info about himself
                return null;

            // https://api.twitch.tv/helix/users/follows?to_id=<user ID>
            var data = await Requests.TwitchJsonRequestAsync($"https://api.twitch.tv/helix/users/follows?from_id={follower.ID}&to_id={channelId}&first=1", RequestType.GET);
            var json = Newtonsoft.Json.Linq.JObject.Parse(data);

            return json["data"].First?.Value<DateTime?>("followed_at");
        }
    }
}
