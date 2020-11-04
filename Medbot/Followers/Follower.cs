using System;
using Newtonsoft.Json;

namespace Medbot.Followers
{
    public class Follower
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; internal set; }

        [JsonProperty(PropertyName = "bio")]
        public string Bio { get; internal set; }

        [JsonProperty(PropertyName = "logo")]
        public string Logo { get; internal set; }

        /// <summary>
        /// Gets Display name of the follower
        /// </summary>
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; internal set; }

        [JsonProperty(PropertyName = "created_at")]
        public DateTime CreatedAt { get; internal set; }

        [JsonProperty(PropertyName = "updated_at")]
        public DateTime UpdatedAt { get; internal set; }

        /// <summary>
        /// Gets ID of the follower
        /// </summary>
        [JsonProperty(PropertyName = "_id")]
        public string ID { get; internal set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; internal set; }
    }
}
