using System;
using System.Threading.Tasks;
using Medbot.Internal;
using Medbot.Enums;
using Medbot.Users;
using System.Linq;

namespace Medbot.Followers
{
    public static class FollowersManager
    {
        /// <summary>
        /// Gets a newest follower
        /// </summary>
        /// <param name="channelId">Channel ID to search</param>
        /// <returns>Follower object of the lastest follower on channel</returns>
        public async static Task<Follower> GetNewestFollower(long channelId)
        {
            var followers = await GetChannelFollowers(channelId, 1);
            return followers?.FirstOrDefault();
        }

        /// <summary>
        /// Gets a information about channel's followers and list of followers
        /// </summary>
        /// <param name="channel">ID of channel to search</param>
        /// <param name="limit">Number of follower to return, max 100</param>
        public async static Task<Follower[]> GetChannelFollowers(long channelId, int limit)
        {
            // https://api.twitch.tv/helix/users/follows?to_id=24395849&first=1
            if (limit > 100) // Can't return more than 100 values
                limit = 100;

            var data = await Requests.TwitchJsonRequestAsync($"https://api.twitch.tv/helix/users/follows?to_id={channelId}&first={limit}", RequestType.GET);
            var json = Newtonsoft.Json.Linq.JObject.Parse(data);

            var followers = json["data"];

            return followers.ToObject<Follower[]>();
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

            return json["data"].First?.ToObject<Follower>().FollowedAt;
        }
    }
}
