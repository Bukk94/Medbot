using Medbot.Users;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace Medbot.Points
{
    public class PointsManager
    {
        private readonly UsersManager _usersManager;
        private readonly ILogger _logger;
        private readonly bool _rewardIdles;
        private Timer timer;

        #region Properties
        /// <summary>
        /// Amount added to users each tick
        /// </summary>
        public int PointsPerTick { get; set; }

        /// <summary>
        /// Idle time before stopping rewarding the user, in minutes
        /// </summary>
        public TimeSpan IdleTime { get; set; }

        /// <summary>
        /// Currency name (e.g. gold)
        /// </summary>
        public static string CurrencyName { get; set; } = String.Empty;

        /// <summary>
        /// Currency plural name
        /// </summary>
        public static string CurrencyNamePlural { get; set; } = String.Empty;

        /// <summary>
        /// Currency units (e.g. G)
        /// </summary>
        public static string CurrencyUnits { get; set; } = String.Empty;

        /// <summary>
        /// If the points timer is running
        /// </summary>
        public bool TimerRunning { get; private set; }

        /// <summary>
        /// Points timer interval
        /// </summary>
        public TimeSpan TimerInterval { get; private set; }

        public int TimerIntervalMs => (int)TimerInterval.TotalMilliseconds;
        #endregion

        /// <summary>
        /// Manages point awarding and timer ticking
        /// </summary>
        /// <param name="usersManager">User manager instance to get access to user information</param>
        /// <param name="interval">The time interval between each tick</param>
        /// <param name="idleTime">Time after which the user will become idle</param>
        /// <param name="rewardIdles">Bool if idle users should be rewarded</param>
        /// <param name="pointsPerTick">Amount of points awarded to active users each tick</param>
        /// <param name="autostart">Bool value if the time should start immediately</param>
        internal PointsManager(UsersManager usersManager, TimeSpan interval, TimeSpan idleTime, bool rewardIdles, int pointsPerTick, bool autostart = false)
        {
            _logger = Logging.GetLogger<PointsManager>();

            _usersManager = usersManager;
            this.TimerInterval = interval;
            this.PointsPerTick = pointsPerTick;
            this.IdleTime = idleTime;
            this._rewardIdles = rewardIdles;
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
                _logger.LogWarning("Can't start the points timer! Timer is already running!");
                return;
            }

            if (this.timer == null)
                this.timer = new Timer(AwardPoints_TimerTick, null, 0, this.TimerIntervalMs);

            this.timer.Change(0, this.TimerIntervalMs);
            this.TimerRunning = true;
            _logger.LogInformation("Points timer started!");
        }

        /// <summary>
        /// Stops points timer
        /// </summary>
        internal void StopPointsTimer()
        {
            if (!TimerRunning)
            {
                _logger.LogWarning("Can't stop the points timer! Timer is not running!");

                return;
            }

            this.timer.Change(Timeout.Infinite, int.MaxValue);
            this.TimerRunning = false;
            _logger.LogInformation("Points timer stopped!");
        }

        /// <summary>
        /// Award points to active users
        /// </summary>
        private void AwardPoints_TimerTick(Object state)
        {
            if (!_usersManager.IsAnyUserOnline)
                return;

            _logger.LogInformation("Points Timer ticked for {count} users.", _usersManager.TotalUsersOnline);
            
            foreach (var user in _usersManager.OnlineUsers)
            {
                if (_usersManager.IsUserBlacklisted(user)) // Skip blacklisted user
                    continue;

                if (user.LastMessage != null && (DateTime.Now - user.LastMessage < TimeSpan.FromMilliseconds(this.IdleTime.TotalMilliseconds) || _rewardIdles))
                {
                    user.AddPoints(this.PointsPerTick);
                    _logger.LogInformation("Rewarding {user} for activity.", user.Username);
                }
            }

            _usersManager.SaveData();
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
