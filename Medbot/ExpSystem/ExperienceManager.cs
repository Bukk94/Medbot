using System;
using System.Collections.Generic;
using System.Threading;
using Medbot.Events;
using Medbot.Internal;
using Medbot.Internal.Models;
using Medbot.Users;
using Microsoft.Extensions.Logging;

namespace Medbot.ExpSystem
{
    internal class ExperienceManager
    {
        private readonly ILogger _logger;
        private readonly BotDataManager _botDataManager;
        private readonly UsersManager _usersManager;
        private readonly ExperienceSettings _experienceSettings;
        private Timer _timer;

        internal event EventHandler<OnRankUpArgs> OnRankUp;

        /// <summary>
        /// List of all available ranks
        /// </summary>
        internal static List<Rank> RankList { get; private set; }

        /// <summary>
        /// If the Experience timer is running
        /// </summary>
        internal bool TimerRunning { get; private set; }

        /// <summary>
        /// Experience timer tick interval
        /// </summary>
        internal TimeSpan ExperienceTickInterval => _experienceSettings.TickInterval;

        internal int ExperienceTickIntervalMs => (int)ExperienceTickInterval.TotalMilliseconds;

        /// <summary>
        /// Value of experience reward for active users
        /// </summary>
        internal int ActiveExperienceReward => _experienceSettings.ActiveExp;

        /// <summary>
        /// Value of experience reward for idle users
        /// </summary>
        internal int IdleExperienceReward => _experienceSettings.IdleExp;

        /// <summary>
        /// Experiences class manages exp awarding and timer ticking
        /// </summary>
        /// <param name="usersManager">User manager instance to get access to user information</param>
        /// <param name="botDataManager">Bot data manager instance to get access to bot information</param>
        /// <param name="autostart">Bool value if timer should start immediately</param>
        internal ExperienceManager(BotDataManager botDataManager, UsersManager usersManager, bool autostart = false)
        {
            _logger = Logging.GetLogger<ExperienceManager>();

            _usersManager = usersManager;
            _botDataManager = botDataManager;
            _experienceSettings = _botDataManager.ExperienceSettings;
            this.TimerRunning = false;

            RankList = _botDataManager.LoadRanks();

            if (autostart)
                StartExperienceTimer();
            else
                _timer = new Timer(AwardExperience_TimerTick, null, Timeout.Infinite, this.ExperienceTickIntervalMs);
        }

        /// <summary>
        /// Starts experience timer
        /// </summary>
        internal void StartExperienceTimer()
        {
            if (TimerRunning)
            {
                _logger.LogWarning("Can't start the experience timer! Timer is already running!");
                return;
            }

            if (_timer == null)
                _timer = new Timer(AwardExperience_TimerTick, null, 0, this.ExperienceTickIntervalMs);

            _timer.Change(0, this.ExperienceTickIntervalMs);
            this.TimerRunning = true;
            _logger.LogInformation("Experience timer started.");
        }

        /// <summary>
        /// Stops experience timer
        /// </summary>
        internal void StopExperienceTimer()
        {
            if (!TimerRunning)
            {
                _logger.LogWarning("Can't stop the experience timer! Timer is not running!");
                return;
            }

            _timer.Change(Timeout.Infinite, int.MaxValue);
            this.TimerRunning = false;
            _logger.LogInformation("Experience timer stopped.");
        }

        /// <summary>
        /// Award experience to users
        /// </summary>
        private void AwardExperience_TimerTick(Object state)
        {
            if (!_usersManager.IsAnyUserOnline)
                return;

            _logger.LogInformation("Experience Timer ticked for {count} users.", _usersManager.TotalUsersOnline);

            foreach (var user in _usersManager.OnlineUsers)
            {
                if (user.LastMessage == null) // Skip users who never wrote anything in chat
                    continue;

                // Skip blacklisted user
                if (_usersManager.IsUserBlacklisted(user))
                    continue;

                // Reward active
                if (DateTime.Now - user.LastMessage < _experienceSettings.IdleTime)
                    user.AddExperience(this.ActiveExperienceReward);
                else // Reward idle
                    user.AddExperience(this.IdleExperienceReward);

                bool newRank = user.CheckRankUp();
                if (newRank && !String.IsNullOrEmpty(_botDataManager.BotDictionary.NewRankMessage))
                    OnRankUp?.Invoke(this, new OnRankUpArgs { User = user, NewRank = user.UserRank });

                _logger.LogInformation("{user} gained experience.", user.DisplayName);
            }

            _usersManager.SaveData();
        }
    }
}
