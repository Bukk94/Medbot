using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Medbot.LoggingNS;
using Medbot.Internal;

namespace Medbot.ExpSystem {
    internal class Experiences {
        private static List<Rank> rankList;
        private bool timerRunning;
        private int activeExp; // Exp value
        private int idleExp; // Exp value
        private TimeSpan interval;
        private TimeSpan idleTime;
        private Timer timer;
        private BotClient bot;

        /// <summary>
        /// Gets a Rank List
        /// </summary>
        internal static List<Rank> RankList { get { return rankList; } } 

        /// <summary>
        /// Gets bool if the Experience timer is running
        /// </summary>
        internal bool TimerRunning { get { return this.timerRunning; } }

        /// <summary>
        /// Gets experience timer tick interval
        /// </summary>
        internal TimeSpan ExperienceInterval { get { return this.interval; } }

        /// <summary>
        /// Gets value of experience for active users
        /// </summary>
        internal int ActiveExperience { get { return this.activeExp; } }

        /// <summary>
        /// Experiences class manages exp awarding and timer ticking
        /// </summary>
        /// <param name="interval">The time interval between each tick in minutes</param>
        /// <param name="activeExp">Number of experience gained by active users</param>
        /// <param name="idleExp">Number of experience gained by idle users</param>
        /// <param name="idleTime">Time after which user will become idle (in minutes)</param>
        /// <param name="autostart">Bool value if timer should start immediately</param>
        internal Experiences(BotClient bot, TimeSpan interval, int activeExp, int idleExp, TimeSpan idleTime, bool autostart = false) {
            this.bot = bot;
            this.interval = interval;
            this.timerRunning = false;
            this.activeExp = activeExp;
            this.idleExp = idleExp;
            this.idleTime = idleTime;
            rankList = new List<Rank>();

            if (autostart)
                StartExperienceTimer();
            else
                this.timer = new Timer(AwardExperience_TimerTick, null, Timeout.Infinite, (int)this.interval.TotalMilliseconds);

            LoadRanks();
        }

        /// <summary>
        /// Loads ranks from txt file
        /// </summary>
        internal void LoadRanks() {
            if (!File.Exists(BotClient.RanksPath)) {
                Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), "FAILED to load ranks. File not found.");
                return;
            }

            string[] dataRaw = File.ReadAllText(BotClient.RanksPath).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int level = 1;

            foreach (string data in dataRaw) {
                // Format Exp (space) rankname:  500 RankName
                try {
                    var rankData = data.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    rankList.Add(new Rank(rankData[1], level++, long.Parse(rankData[0])));
                } catch(Exception ex) {
                    Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                    continue;
                }
            }
        }

        /// <summary>
        /// Starts experience timer
        /// </summary>
        internal void StartExperienceTimer() {
            if (TimerRunning) {
                Console.WriteLine("Experience timer is already running");
                return;
            }

            if (this.timer == null)
                this.timer = new Timer(AwardExperience_TimerTick, null, 0, (int)this.interval.TotalMilliseconds);

            this.timer.Change(0, (int)this.interval.TotalMilliseconds);
            this.timerRunning = true;
            Console.WriteLine("Starting experience timer");
        }

        /// <summary>
        /// Stops experience timer
        /// </summary>
        internal void StopExperienceTimer() {
            if (!TimerRunning) {
                Console.WriteLine("Timer is not running");
                return;
            }

            this.timer.Change(Timeout.Infinite, int.MaxValue);
            this.timerRunning = false;
            Console.WriteLine("Stopping experience timer");
        }

        /// <summary>
        /// Award experience to users
        /// </summary>
        private void AwardExperience_TimerTick(Object state) {
            if (BotClient.OnlineUsers == null || BotClient.OnlineUsers.Count <= 0)
                return;

            Console.WriteLine("Timer Experience ticked for " + BotClient.OnlineUsers.Count + " users");

            foreach (User u in BotClient.OnlineUsers) {
                if (u.LastMessage == null) // Skip users who never wrote anything in chat
                    continue;

                // Skip blacklisted user
                if (BotClient.UserBlacklist.Contains(u.Username))
                    continue;

                // Reward active
                if (DateTime.Now - u.LastMessage < TimeSpan.FromMilliseconds(this.idleTime.TotalMilliseconds))
                    u.AddExperience(this.activeExp);
                else // Reward idle
                    u.AddExperience(this.idleExp);

                bool newRank = u.CheckRankUp();
                if (newRank && !String.IsNullOrEmpty(BotDictionary.NewRankMessage))
                    this.bot.SendChatMessage(String.Format(BotDictionary.NewRankMessage, u.DisplayName, u.UserRank.RankLevel, u.UserRank.RankName));

                Console.WriteLine(u.DisplayName + " gained experience");
            }
            
            FilesControl.SaveData();
        }
    }
}
