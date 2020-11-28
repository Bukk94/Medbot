using System;
using System.Threading.Tasks;
using Medbot.Internal;
using Medbot.Enums;
using Medbot.Users;
using System.Linq;

namespace Medbot.Followers
{
    public class FollowersManager
    {
        /// <summary>
        /// Gets a newest follower
        /// </summary>
        /// <param name="channelId">Channel ID to search</param>
        /// <returns>Follower object of the lastest follower on the channel</returns>
        public async Task<Follower> GetNewestFollower(long channelId)
        {
            var followers = await GetChannelFollowers(channelId, 1);
            return followers?.FirstOrDefault();
        }

        /// <summary>
        /// Gets a information about channel's followers
        /// </summary>
        /// <param name="channel">ID of channel to search</param>
        /// <param name="limit">Number of followers to return, max 100</param>
        public async Task<Follower[]> GetChannelFollowers(long channelId, int limit)
        {
            // https://api.twitch.tv/helix/users/follows?to_id=24395849&first=1
            if (limit > 100) // Can't return more than 100 values
                limit = 100;

            var data = await Requests.TwitchJsonRequestAsync($"https://api.twitch.tv/helix/users/follows?to_id={channelId}&first={limit}", RequestType.GET);
            var followers = Requests.GetJsonData(data);

            return followers.ToObject<Follower[]>();
        }

        /// <summary>
        /// Get date when user followed the channel 
        /// </summary>
        /// <param name="channel">ID of channel to search</param>
        /// <param name="user">User to get info about</param>
        /// <returns>Return nullable datetime</returns>
        public async Task<DateTime?> GetFollowDate(long channelId, User user)
        {
            if (user.ID <= 0)
                await user.UpdateUserId();

            if (channelId == user.ID) // return if owner is trying to get info about himself
                return null;

            // https://api.twitch.tv/helix/users/follows?to_id=<user ID>
            var data = await Requests.TwitchJsonRequestAsync($"https://api.twitch.tv/helix/users/follows?from_id={user.ID}&to_id={channelId}&first=1", RequestType.GET);
            
            return Requests.GetJsonData(data)?.FirstOrDefault()?.ToObject<Follower>().FollowedAt;
        }
    }
}
