using System;
using System.Collections.Generic;
using System.Linq;
using Medbot.ExpSystem;
using Medbot.Exceptions;

namespace Medbot {
    public class User {
        private readonly string nickname;
        private string displayName;
        private DateTime? lastMessage;
        private long points;
        private bool mod;
        private bool broadcaster;
        private bool subscriber;
        private Rank rank;
        private long experience;

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
        public DateTime? LastMessage { get { return this.lastMessage; } set { this.lastMessage = value; } }

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
        /// Gets/Sets a user's current rank
        /// </summary>
        public Rank UserRank { get { return this.rank; } set { this.rank = value; } }

        /// <summary>
        /// Gets user's total experience
        /// </summary>
        public long Experience { get { return this.experience; } set { this.experience = value; } }

        /// <summary>
        /// Data structure containing information about User such as username, points, experience, rank and time of last sent message
        /// </summary>
        /// <param name="nickname">Username of the User</param>
        public User(string nickname, Rank rank = null, bool mod = false, bool subscriber = false, bool owner = false) {
            this.nickname = nickname;
            this.displayName = nickname;
            this.rank = rank;
            this.mod = mod;
            this.subscriber = subscriber;
            this.broadcaster = owner;
            lastMessage = null;
            points = 0;
            experience = 0;
        }

        /// <summary>
        /// Data structure containing information about User such as username, points, experience, rank and time of last sent message
        /// </summary>
        /// <param name="nickname">Username of the User</param>
        /// <param name="points">Number of user's points</param>
        /// <param name="experience">Number of user's experience</param>
        public User(string nickname, long points, long experience) {
            this.nickname = nickname;
            this.displayName = nickname;
            this.points = points;
            this.experience = experience;
        }

        /// <summary>
        /// Data structure containing information about User such as username, points, experience, rank and time of last sent message
        /// </summary>
        /// <param name="nickname">Username of the User</param>
        /// <param name="points">Number of user's points</param>
        public User(string nickname, long points) {
            this.nickname = nickname;
            this.displayName = nickname;
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

        /// <summary>
        /// Trades user points. If fail, throws PointsException
        /// </summary>
        /// <param name="points"></param>
        /// <exception cref="PointsException">When user doesn'T have enough points</exception>
        public void Trade(long points, User target, string targetUsername) {
            if (this.Points - points >= 0) { // User is able to trade
                RemovePoints(points);

                if (target == null) { // Target is not online, trade into file
                    FilesControl.AddUserPointsToFile(targetUsername, points);
                } else { // User is online
                    target.AddPoints(points);
                    FilesControl.SaveData();
                }
            } else {
                throw new PointsException("User can't trade this amount of points. User doesn't have enough points to trade.");
            }
        }

        /// <summary>
        /// Adds number of experince to user's total
        /// </summary>
        /// <param name="value">Long - number of exp to add</param>
        public void AddExperience(long value) {
            this.experience += value;
        }
         
        /// <summary>
        /// Checks if the user is able to rank up, if yes, promote him to next rank
        /// </summary>
        /// <returns>Return bool if user got new rank up</returns>
        public bool CheckRankUp() {
            bool nullRank = this.rank == null;
            Rank matchRank = Experiences.RankList.Last(r => r.ExpRequired <= this.Experience);
            
            if (matchRank != this.rank) { // Gain new rank
                this.rank = matchRank;

                if (!nullRank) // Skip initiate null value (loading data from file)
                    return true;    
            }
            return false;
        }

        /// <summary>
        /// Returns how much experience user need to gain new rank
        /// </summary>
        /// <returns>long value</returns>
        public long ToNextRank() {
            if (this.rank == null)
                return 0;

            Rank next = NextRank();
            return next != null ? next.ExpRequired - this.experience : 0;
        }

        /// <summary>
        /// Gets time needed for rankup
        /// </summary>
        /// <param name="xpRate">Rate of gaining experiences</param>
        /// <param name="interval">Experience timer interval</param>
        /// <returns>Returns double value with 2 decimal places</returns>
        public string TimeToNextRank(int xpRate, TimeSpan interval) {
            //  exp / activeExp / (60/intervalMinutes) -> hours
            //  exp / activeExp / (60/intervalMinutes) * 60 -> minutes

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
        public Rank NextRank() {
            var rankIndex = Experiences.RankList.IndexOf(this.rank);
            return rankIndex + 1 < Experiences.RankList.Count - 1 ? Experiences.RankList[rankIndex + 1] : null;
        }
    }

    public class TempUser {
        public string Username { get; set; }
        public string Data { get; set; }

        /// <summary>
        /// Data structure used for temporary User objects
        /// </summary>
        /// <param name="name">User's nickname</param>
        /// <param name="data">XML data</param>
        public TempUser(string name, string data) {
            this.Username = name;
            Data = data;
        }
    }
}
