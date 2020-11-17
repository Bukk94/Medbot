using System;
using System.Collections.Generic;
using System.Linq;
using Medbot.ExpSystem;
using Medbot.Enums;

namespace Medbot.Users
{
    public class User
    {
        #region Properties
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

        // TODO: Maybe rework this method? It's doing check and rankup at the same time!
        /// <summary>
        /// Checks if the user is able to rank up, if yes, promote him to next rank
        /// </summary>
        /// <returns>Return bool if user got new rank up</returns>
        public bool CheckRankUp()
        {
            bool nullRank = this.UserRank == null;
            Rank matchRank = ExperienceManager.RankList.Last(r => r.ExpRequired <= this.Experience);

            if (matchRank != this.UserRank)
            { // Gain new rank
                this.UserRank = matchRank;

                if (!nullRank) // Skip initiate null value (loading data from file)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns how much experience user need to gain new rank
        /// </summary>
        /// <returns>long value</returns>
        public long ToNextRank()
        {
            if (this.UserRank == null)
                return 0;

            Rank next = NextRank();
            return next != null ? next.ExpRequired - this.Experience : 0;
        }

        /// <summary>
        /// Gets time needed for rankup
        /// </summary>
        /// <param name="xpRate">Rate of gaining experiences</param>
        /// <param name="interval">Experience timer interval</param>
        /// <returns>Returns double value with 2 decimal places</returns>
        public string TimeToNextRank(int xpRate, TimeSpan interval)
        {
            //  exp / activeExp / (60/intervalMinutes) -> hours
            //  exp / activeExp / (60/intervalMinutes) * 60 -> minutes

            // TODO: Cleanup - wth is this?
            long nextRank = ToNextRank();
            double hours = Math.Round((double)this.ToNextRank() / xpRate / (60 / interval.TotalMinutes), 0);
            double minutes = Math.Round((double)this.ToNextRank() / xpRate / (60 / interval.TotalMinutes) * 60, 0) - (hours * 60);

            if (hours <= 0)
                return minutes + " min";
            else if (minutes <= 0)
                return hours + " h";

            return String.Format("{0} h {1} min", hours, minutes);
        }

        /// <summary>
        /// Gets user's next rank
        /// </summary>
        /// <returns>Return user's next rank as Rank object</returns>
        public Rank NextRank()
        {
            var rankIndex = ExperienceManager.RankList.IndexOf(this.UserRank);
            return rankIndex + 1 < ExperienceManager.RankList.Count - 1 ? ExperienceManager.RankList[rankIndex + 1] : null;
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
