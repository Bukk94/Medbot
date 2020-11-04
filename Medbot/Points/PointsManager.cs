using System;
using System.Threading;

namespace Medbot.Points {
    public class PointsManager {
        private bool rewardIdles;
        private bool timerRunning;
        private TimeSpan interval;
        private TimeSpan idleTime;
        private int pointsPerTick;
        private Timer timer;
        private static string currencyName = String.Empty;
        private static string currencyNamePlural = String.Empty;
        private static string currencyUnits = String.Empty;

        /// <summary>
        /// Amount added to users each tick
        /// </summary>
        public int PointsPerTick { get { return this.pointsPerTick; } set { this.pointsPerTick = value; } }

        /// <summary>
        /// Sets/Gets Idle time before stopping rewarding the user, in minutes
        /// </summary>
        public TimeSpan IdleTime { get { return this.idleTime; } set { this.idleTime = value; } }

        /// <summary>
        /// Gets/Sets currency name
        /// </summary>
        public static string CurrencyName { get { return currencyName; } set { currencyName = value; } }

        /// <summary>
        /// Gets/Sets currency plural name
        /// </summary>
        public static string CurrencyNamePlural { get { return currencyNamePlural; } set { currencyNamePlural = value; } }

        /// <summary>
        /// Gets/Sets currency units
        /// </summary>
        public static string CurrencyUnits { get { return currencyUnits; } set { currencyUnits = value; } }

        /// <summary>
        /// Gets bool if the Points timer is running
        /// </summary>
        public bool TimerRunning { get { return this.timerRunning; } }

        /// <summary>
        /// Gets points timer interval
        /// </summary>
        public TimeSpan TimerInterval { get { return this.interval; } }

        /// <summary>
        /// Point class manages point awarding and timer ticking
        /// </summary>
        /// <param name="onlineUsers">Reference to List of online users</param>
        /// <param name="interval">The time interval between each tick</param>
        /// <param name="rewardIdles">Bool if idle users should be rewarded</param>
        /// <param name="idleTime">Time after which the user will become idle</param>
        /// <param name="pointsPerTick">Amount of points awarded to active users each tick</param>
        /// <param name="autostart">Bool value if the time should start immediately</param>
        internal PointsManager(TimeSpan interval, TimeSpan idleTime, bool rewardIdles, int pointsPerTick, bool autostart = false) {
            this.interval = interval;
            this.pointsPerTick = pointsPerTick;
            this.idleTime = idleTime;
            this.rewardIdles = rewardIdles;
            this.timerRunning = false;

            if (autostart)
                StartPointsTimer();
            else
                this.timer = new Timer(AwardPoints_TimerTick, null, Timeout.Infinite, (int)this.interval.TotalMilliseconds);
        }

        /// <summary>
        /// Starts points timer
        /// </summary>
        internal void StartPointsTimer() {
            if (TimerRunning) {
                Console.WriteLine("Points timer is already running");
                return;
            }

            if(this.timer == null)
                this.timer = new Timer(AwardPoints_TimerTick, null, 0, (int)this.interval.TotalMilliseconds);

            this.timer.Change(0, (int)this.interval.TotalMilliseconds);
            this.timerRunning = true;
            Console.WriteLine("Starting points timer");
        }

        /// <summary>
        /// Stops points timer
        /// </summary>
        internal void StopPointsTimer() {
            if (!TimerRunning) {
                Console.WriteLine("Timer is not running");
                return;
            }

            this.timer.Change(Timeout.Infinite, int.MaxValue);
            this.timerRunning = false;
            Console.WriteLine("Stopping points timer");
        }

        /// <summary>
        /// Award points to active users
        /// </summary>
        private void AwardPoints_TimerTick(Object state) {
            if (BotClient.OnlineUsers == null || BotClient.OnlineUsers.Count <= 0)
                return;

            Console.WriteLine("Timer Points ticked, Number of users: " + BotClient.OnlineUsers.Count);
            foreach (User u in BotClient.OnlineUsers) {
                if (BotClient.UserBlacklist.Contains(u.Username)) // Skip blacklisted user
                    continue;

                if (u.LastMessage != null && (DateTime.Now - u.LastMessage < TimeSpan.FromMilliseconds(this.idleTime.TotalMilliseconds) || rewardIdles)) {
                    u.AddPoints(this.pointsPerTick);
                    Console.WriteLine("Rewarding " + u.Username + " for activity");
                }
            }

            FilesControl.SaveData();
        }

        /// <summary>
        /// Loads default currency details
        /// </summary>
        internal static void LoadDefaultCurrencyDetails() {
            CurrencyName = "gold";
            CurrencyNamePlural = "gold";
            CurrencyUnits = "g";
        }
    }
}
