using Newtonsoft.Json;
using System.Collections.Generic;

namespace Medbot.Followers {
    public class FollowersInfo {
        /// <summary>
        /// Gets total number of followers
        /// </summary>
        [JsonProperty(PropertyName = "_total")]
        public int Total { get; internal set; }

        /// <summary>
        /// Gets string ID of cursor
        /// </summary>
        [JsonProperty(PropertyName = "_cursor")]
        public string Cursor { get; internal set; }

        /// <summary>
        /// Gets list of followers
        /// </summary>
        [JsonProperty(PropertyName = "follows")]
        public List<Follow> FollowersList { get; internal set; }
    }
}
