using Medbot.Internal;
using Medbot.Internal.Models;
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
        private readonly CurrencySettings _currencySettings;
        private Timer _timer;

        #region Properties
        /// <summary>
        /// If the points timer is running
        /// </summary>
        public bool IsTimerRunning { get; private set; }

        /// <summary>
        /// Amount added to users each tick
        /// </summary>
        public int PointsPerTick => _currencySettings.PointsPerTick;

        /// <summary>
        /// Idle time before stopping rewarding the user, in minutes
        /// </summary>
        public TimeSpan IdleTime => _currencySettings.IdleTime;

        /// <summary>
        /// Points timer interval
        /// </summary>
        public TimeSpan TimerInterval => _currencySettings.TickInterval;

        public int TimerIntervalMs => (int)TimerInterval.TotalMilliseconds;

        /// <summary>
        /// Currency name (e.g. 'gold')
        /// </summary>
        public string CurrencyName => _currencySettings.Name;

        /// <summary>
        /// Currency plural name
        /// </summary>
        public string CurrencyNamePlural => _currencySettings.Plural;

        /// <summary>
        /// Currency units (e.g. 'G')
        /// </summary>
        public string CurrencyUnits => _currencySettings.Units;
        #endregion

        /// <summary>
        /// Manages point awarding and timer ticking
        /// </summary>
        /// <param name="botDataManager">Bot data manager instance to get access to bot information</param>
        /// <param name="usersManager">User manager instance to get access to user information</param>
        /// <param name="autostart">Bool value if the time should start immediately</param>
        internal PointsManager(BotDataManager botDataManager, UsersManager usersManager, bool autostart = false)
        {
            _logger = Logging.GetLogger<PointsManager>();
            _usersManager = usersManager;
            _currencySettings = botDataManager.CurrencySettings;

            this.IsTimerRunning = false;

            if (autostart)
                StartPointsTimer();
            else
                _timer = new Timer(AwardPoints_TimerTick, null, Timeout.Infinite, this.TimerIntervalMs);
        }

        /// <summary>
        /// Starts points timer
        /// </summary>
        internal void StartPointsTimer()
        {
            if (IsTimerRunning)
            {
                _logger.LogWarning("Can't start the points timer! Timer is already running!");
                return;
            }

            if (this._timer == null)
                this._timer = new Timer(AwardPoints_TimerTick, null, 0, this.TimerIntervalMs);

            this._timer.Change(0, this.TimerIntervalMs);
            this.IsTimerRunning = true;
            _logger.LogInformation("Points timer started!");
        }

        /// <summary>
        /// Stops points timer
        /// </summary>
        internal void StopPointsTimer()
        {
            if (!IsTimerRunning)
            {
                _logger.LogWarning("Can't stop the points timer! Timer is not running!");

                return;
            }

            this._timer.Change(Timeout.Infinite, int.MaxValue);
            this.IsTimerRunning = false;
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

                if (user.LastMessage != null && (DateTime.Now - user.LastMessage < TimeSpan.FromMilliseconds(this.IdleTime.TotalMilliseconds) || _currencySettings.RewardIdles))
                {
                    user.AddPoints(this.PointsPerTick);
                    _logger.LogInformation("Rewarding {user} for activity.", user.Username);
                }
            }

            _usersManager.SaveData();
        }
    }
}
