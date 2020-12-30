using System;
using System.Collections.Generic;
using System.Linq;
using Medbot.ExpSystem;
using Medbot.Enums;
using Medbot.Internal;
using System.Threading.Tasks;

namespace Medbot.Users
{
    public class User
    {
        #region Properties
        public long ID { get; set; } = -1;

        /// <summary>
        /// Username of the user in lowercase
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Sets/Gets User's Display name in chat, including lower and upper case
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Date and Time of last message user sent. Can be null.
        /// </summary>
        public DateTime? LastMessage { get; set; }

        /// <summary>
        /// User's watching points
        /// </summary>
        public long Points { get; set; }

        /// <summary>
        /// Gets bool if the user is Broadcaster
        /// </summary>
        public bool IsBroadcaster { get; private set; }

        /// <summary>
        /// Gets bool if the user is chat Moderator 
        /// </summary>
        public bool IsModerator { get; private set; }

        // UNUSED: Subscriber value
        /// <summary>
        /// Gets bool if the user is subscribed to the channel
        /// </summary>
        public bool IsSubscriber { get; private set; }

        /// <summary>
        /// Gets/Sets a user's current rank
        /// </summary>
        public Rank UserRank { get; set; }

        /// <summary>
        /// Gets user's total experience
        /// </summary>
        public long Experience { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Data structure containing information about User such as username, points, experience, rank and time of last sent message
        /// </summary>
        /// <param name="nickname">Username of the User</param>
        public User(string nickname, Rank rank = null, bool mod = false, bool subscriber = false, bool owner = false)
        {
            this.Username = nickname;
            this.DisplayName = nickname;
            this.UserRank = rank;
            this.IsModerator = mod;
            this.IsSubscriber = subscriber;
            this.IsBroadcaster = owner;
            LastMessage = null;
            Points = 0;
            Experience = 0;
        }

        /// <summary>
        /// Data structure containing information about User such as username, points, experience, rank and time of last sent message
        /// </summary>
        /// <param name="nickname">Username of the User</param>
        /// <param name="points">Number of user's points</param>
        /// <param name="experience">Number of user's experience</param>
        public User(string nickname, long points, long experience)
        {
            this.Username = nickname;
            this.DisplayName = nickname;
            this.Points = points;
            this.Experience = experience;
        }

        /// <summary>
        /// Data structure containing information about User such as username, points, experience, rank and time of last sent message
        /// </summary>
        /// <param name="nickname">Username of the User</param>
        /// <param name="points">Number of user's points</param>
        public User(string nickname, long points)
        {
            this.Username = nickname;
            this.DisplayName = nickname;
            this.Points = points;
        }
        #endregion

        /// <summary>
        /// Applies all given badges to user
        /// </summary>
        /// <param name="badges">List of badges</param>
        public void ApplyBadges(List<Badges> badges)
        {
            if (badges?.Any() != true)
                return;

            this.IsBroadcaster = badges.Any(x => x.Equals(Badges.Broadcaster));
            this.IsModerator = badges.Any(x => x.Equals(Badges.Moderator));
            this.IsSubscriber = badges.Any(x => x.Equals(Badges.Subscriber));
        }

        /// <summary>
        /// Adds number of points to total of user's points
        /// </summary>
        /// <param name="value">Long - number of points to be added</param>
        public void AddPoints(long value)
        {
            this.Points += value;
        }

        /// <summary>
        /// Removes number of points from user's points
        /// </summary>
        /// <param name="value">Long - number of points to be removed</param>
        public void RemovePoints(long value)
        {
            if (this.Points - value >= 0)
                this.Points -= value;
            else
                this.Points = 0;
        }

        /// <summary>
        /// Adds number of experince to user's total
        /// </summary>
        /// <param name="value">Long - number of exp to add</param>
        public void AddExperience(long value)
        {
            this.Experience += value;
        }

        public async Task UpdateUserId()
        {
            this.ID = await Requests.GetUserId(this.Username);
        }
    }

    public class TempUser
    {
        public string Username { get; set; }
        public string Data { get; set; }

        /// <summary>
        /// Data structure used for temporary User objects
        /// </summary>
        /// <param name="name">User's nickname</param>
        /// <param name="data">XML data</param>
        public TempUser(string name, string data)
        {
            this.Username = name;
            Data = data;
        }
    }
}
