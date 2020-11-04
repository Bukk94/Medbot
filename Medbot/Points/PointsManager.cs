﻿using System;
using System.Threading;

namespace Medbot.Points
{
    public class PointsManager
    {
        private readonly bool rewardIdles;
        private TimeSpan idleTime;
        private Timer timer;

        /// <summary>
        /// Amount added to users each tick
        /// </summary>
        public int PointsPerTick { get; set; }

        /// <summary>
        /// Sets/Gets Idle time before stopping rewarding the user, in minutes
        /// </summary>
        public TimeSpan IdleTime
        {
            get { return this.idleTime; }
            set { this.idleTime = value; }
        }

        /// <summary>
        /// Gets/Sets currency name
        /// </summary>
        public static string CurrencyName { get; set; } = String.Empty;

        /// <summary>
        /// Gets/Sets currency plural name
        /// </summary>
        public static string CurrencyNamePlural { get; set; } = String.Empty;

        /// <summary>
        /// Gets/Sets currency units
        /// </summary>
        public static string CurrencyUnits { get; set; } = String.Empty;

        /// <summary>
        /// Gets bool if the Points timer is running
        /// </summary>
        public bool TimerRunning { get; private set; }

        /// <summary>
        /// Gets points timer interval
        /// </summary>
        public TimeSpan TimerInterval { get; private set; }

        public int TimerIntervalMs => (int)TimerInterval.TotalMilliseconds;

        /// <summary>
        /// Point class manages point awarding and timer ticking
        /// </summary>
        /// <param name="onlineUsers">Reference to List of online users</param>
        /// <param name="interval">The time interval between each tick</param>
        /// <param name="rewardIdles">Bool if idle users should be rewarded</param>
        /// <param name="idleTime">Time after which the user will become idle</param>
        /// <param name="pointsPerTick">Amount of points awarded to active users each tick</param>
        /// <param name="autostart">Bool value if the time should start immediately</param>
        internal PointsManager(TimeSpan interval, TimeSpan idleTime, bool rewardIdles, int pointsPerTick, bool autostart = false)
        {
            this.TimerInterval = interval;
            this.PointsPerTick = pointsPerTick;
            this.idleTime = idleTime;
            this.rewardIdles = rewardIdles;
            this.TimerRunning = false;

            if (autostart)
                StartPointsTimer();
            else
                this.timer = new Timer(AwardPoints_TimerTick, null, Timeout.Infinite, this.TimerIntervalMs);
        }

        /// <summary>
        /// Starts points timer
        /// </summary>
        internal void StartPointsTimer()
        {
            if (TimerRunning)
            {
                Console.WriteLine("Points timer is already running");
                return;
            }

            if (this.timer == null)
                this.timer = new Timer(AwardPoints_TimerTick, null, 0, this.TimerIntervalMs);

            this.timer.Change(0, this.TimerIntervalMs);
            this.TimerRunning = true;
            Console.WriteLine("Starting points timer");
        }

        /// <summary>
        /// Stops points timer
        /// </summary>
        internal void StopPointsTimer()
        {
            if (!TimerRunning)
            {
                Console.WriteLine("Timer is not running");
                return;
            }

            this.timer.Change(Timeout.Infinite, int.MaxValue);
            this.TimerRunning = false;
            Console.WriteLine("Stopping points timer");
        }

        /// <summary>
        /// Award points to active users
        /// </summary>
        private void AwardPoints_TimerTick(Object state)
        {
            if (BotClient.OnlineUsers == null || BotClient.OnlineUsers.Count <= 0)
                return;

            Console.WriteLine("Timer Points ticked, Number of users: " + BotClient.OnlineUsers.Count);
            foreach (User u in BotClient.OnlineUsers)
            {
                if (BotClient.UserBlacklist.Contains(u.Username)) // Skip blacklisted user
                    continue;

                if (u.LastMessage != null && (DateTime.Now - u.LastMessage < TimeSpan.FromMilliseconds(this.idleTime.TotalMilliseconds) || rewardIdles))
                {
                    u.AddPoints(this.PointsPerTick);
                    Console.WriteLine("Rewarding " + u.Username + " for activity");
                }
            }

            FilesControl.SaveData();
        }

        /// <summary>
        /// Loads default currency details
        /// </summary>
        internal static void LoadDefaultCurrencyDetails()
        {
            CurrencyName = "gold";
            CurrencyNamePlural = "gold";
            CurrencyUnits = "g";
        }
    }
}
