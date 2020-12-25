using Medbot.Commands;
using Medbot.ExpSystem;
using Medbot.Internal.Models;
using Medbot.Users;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Medbot.Internal
{
    internal class BotDataManager
    {
        private readonly ILogger _logger;
        private readonly FilesControl _filesControl;
        private AllSettings _allSettings;

        internal bool IsBotModerator => BotObject?.IsModerator ?? false;

        internal User BotObject { get; set; }

        internal DictionaryStrings BotDictionary { get; private set; }

        internal LoginDetails Login { get; private set; }
        
        internal BotSettings BotSettings { get; private set; }

        internal CurrencySettings CurrencySettings { get; private set; }

        internal ExperienceSettings ExperienceSettings { get; private set; }

        /// <summary>
        /// Can bot use colored messages?
        /// </summary>
        public bool UseColoredMessages { get; set; }

        public BotDataManager()
        {
            _filesControl = new FilesControl();
            _logger = Logging.GetLogger<BotDataManager>();

            _allSettings = _filesControl.LoadAllSettings();
            _logger.LogInformation("Bot settings loaded successfully.");

            this.LoadLoginCredentials();
            this.LoadBotSettings();
            this.LoadBotIntervals();
            this.BotDictionary = _filesControl.LoadBotDictionary();

            // TODO: Make this configurable
            UseColoredMessages = true;
        }

        private void CheckLoadedSettings()
        {
            if (_allSettings == null)
                _allSettings = _filesControl.LoadAllSettings();
        }

        private void LoadBotIntervals()
        {
            CheckLoadedSettings();
            this.CurrencySettings = _allSettings.Currency;
            this.ExperienceSettings = _allSettings.Experience;
        }

        private void LoadLoginCredentials()
        {
            CheckLoadedSettings();

            this.Login = _allSettings.Login;
            this.Login.VerifyLoginCredentials();
            Requests.LoginDetails = this.Login;
            this.Login.ChannelId = Task.Run(() => Requests.GetUserId(this.Login.Channel)).Result;
        }

        private void LoadBotSettings()
        {
            CheckLoadedSettings();

            this.BotSettings = _allSettings.Settings;

            // Percentage is incorrectly set, exceeding 100%. Load default
            if (this.BotSettings.GambleBonusWinPercentage + this.BotSettings.GambleWinPercentage >= 100)
            {
                this.BotSettings.GambleWinPercentage = 20;
                this.BotSettings.GambleBonusWinPercentage = 2;
            }
        }

        internal List<string> LoadUsersBlacklist()
        {
            CheckLoadedSettings();
            return _allSettings.Blacklist ?? new List<string>();
        }

        // TODO: Delete this and create Commands list here
        internal List<Command> LoadCommands()
        {
            return _filesControl.LoadCommands();
        }

        #region FileControl method redirects
        internal void SaveDataInternal(List<User> onlineUsers)
        {
            _filesControl.SaveData(onlineUsers);
        }

        internal User LoadUserData(User user)
        {
            return Task.Run(() => _filesControl.LoadUserData(user)).Result;
        }

        internal List<TempUser> GetPointsLeaderboard()
        {
            return _filesControl.GetPointsLeaderboard(Login.BotName);
        }

        internal List<TempUser> GetExperienceLeaderboard()
        {
            return _filesControl.GetExperienceLeaderboard(Login.BotName);
        }

        internal List<Rank> LoadRanks()
        {
            return _filesControl.LoadRanks();
        }

        internal void AddUserPointsToFile(string username, long pointsToAdd)
        {
            _filesControl.AddUserPointsToFile(username, pointsToAdd);
        }

        internal void RemoveUserPointsFromFile(string username, long pointsToRemove)
        {
            _filesControl.RemoveUserPointsFromFile(username, pointsToRemove);
        }

        internal void AddUserExperienceToFile(string username, long experienceToAdd)
        {
            _filesControl.AddUserExperienceToFile(username, experienceToAdd);
        }
        #endregion

        /// <summary>
        /// Updates bot permissions
        /// </summary>
        /// <param name="botObject">Bot's user object</param>
        internal void UpdateBotPermissions(User botObject)
        {
            if (botObject == null || botObject.IsModerator == IsBotModerator) // Do nothing if botObject is null or permissions were not changed
                return;

            BotObject = botObject;
        }
    }
}
