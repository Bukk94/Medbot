using Newtonsoft.Json;
using System;

namespace Medbot.Followers
{
    public class Follower
    {
        [JsonProperty(PropertyName = "from_id")]
        public int FollowerId { get; internal set; }

        [JsonProperty(PropertyName = "from_name")]
        public string FollowerName { get; internal set; }

        [JsonProperty(PropertyName = "to_id")]
        public int ChannelId { get; internal set; }

        [JsonProperty(PropertyName = "to_name")]
        public string ChannelName { get; internal set; }

        [JsonProperty(PropertyName = "followed_at")]
        public DateTime FollowedAt { get; internal set; }
    }
}
