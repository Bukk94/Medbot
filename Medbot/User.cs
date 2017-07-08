using System;
using System.Collections.Generic;
using System.Linq;

namespace Medbot {
    public class User {
        private readonly string nickname;
        private string displayName;
        private Nullable<DateTime> lastMessage;
        private long points;
        private bool mod;
        private bool broadcaster;
        private bool subscriber;

        /// <summary>
        /// Username of the user in lowercase
        /// </summary>
        public string Username { get { return this.nickname; } }

        /// <summary>
        /// Sets/Gets User's Display name in chat, including lower and upper case
        /// </summary>
        public string DisplayName { get { return this.displayName; } set { this.displayName = value; } }

        /// <summary>
        /// Date and Time of last message user sent. Can be null.
        /// </summary>
        public Nullable<DateTime> LastMessage { get { return this.lastMessage; } set { this.lastMessage = value; } }

        /// <summary>
        /// User's watching points
        /// </summary>
        public long Points { get { return points; } set { this.points = value; } }

        /// <summary>
        /// Gets bool if the user is Broadcaster
        /// </summary>
        public bool Broadcaster { get { return this.broadcaster; } }

        /// <summary>
        /// Gets bool if the user is chat Moderator 
        /// </summary>
        public bool Moderator { get { return this.mod; } }

        // UNUSED: Subscriber value
        /// <summary>
        /// Gets bool if the user is subscribed to the channel
        /// </summary>
        public bool Subscriber { get { return this.subscriber; } }

        /// <summary>
        /// Data structure containing information about User such as username, points and time of last sent message
        /// </summary>
        /// <param name="nickname">Username of the User</param>
        public User(string nickname, bool mod = false, bool subscriber = false, bool owner = false) {
            this.nickname = nickname;
            this.displayName = nickname;
            this.mod = mod;
            this.subscriber = subscriber;
            this.broadcaster = owner;
            lastMessage = null;
            points = 0;
        }

        public User(string nickname, long points) {
            this.nickname = nickname;
            this.points = points;
        }

        /// <summary>
        /// Applies all given badges to user
        /// </summary>
        /// <param name="badges">List of badges</param>
        public void ApplyBadges(List<string> badges) {
            if (badges == null)
                return;

            if (badges.SingleOrDefault(b => b.Contains("broadcaster")) != null)
                this.broadcaster = true;

            if (badges.SingleOrDefault(b => b.Contains("moderator")) != null)
                this.mod = true;

            if (badges.SingleOrDefault(b => b.Contains("subscriber")) != null)
                this.subscriber = true;
        }

        /// <summary>
        /// Adds number of points to total of user's points
        /// </summary>
        /// <param name="value">Long - number of points to be added</param>
        public void AddPoints(long value) {
            this.points += value;
        }

        /// <summary>
        /// Removes number of points from user's points
        /// </summary>
        /// <param name="value">Long - number of points to be removed</param>
        public void RemovePoints(long value) {
            if (this.points - value >= 0)
                this.points -= value;
            else
                this.points = 0;
        }
    }
}
