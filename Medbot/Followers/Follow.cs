using Newtonsoft.Json;
using System;

namespace Medbot.Followers {
    public class Follow {
        /// <summary>
        /// Gets information about DateTime of follow creation
        /// </summary>
        [JsonProperty(PropertyName = "created_at")]
        public DateTime CreatedAt { get; internal set; }

        /// <summary>
        /// Gets bool if the notification for the follower is enabled
        /// </summary>
        [JsonProperty(PropertyName = "notifications")]
        public bool Notifications { get; internal set; }

        /// <summary>
        /// Gets information about Follower
        /// </summary>
        [JsonProperty(PropertyName = "user")]
        public Follower Follower { get; internal set; }
    }
}
