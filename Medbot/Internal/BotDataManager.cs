using Medbot.Commands;
using Medbot.ExpSystem;
using Medbot.Internal.Models;
using Medbot.Users;
using System.Collections.Generic;

namespace Medbot.Internal
{
    internal class BotDataManager
    {
        private readonly FilesControl _filesControl;

        internal bool IsBotModerator => BotObject?.IsModerator ?? false;

        internal User BotObject { get; set; }

        internal Dictionary<string, int> BotIntervals { get; private set; }

        internal DictionaryStrings BotDictionary { get; private set; }

        public BotDataManager()
        {
            _filesControl = new FilesControl();

            _filesControl.LoadLoginCredentials().Wait();
            _filesControl.LoadBotSettings();
            BotDictionary = _filesControl.LoadBotDictionary();
            BotIntervals = _filesControl.LoadBotIntervals();
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

        internal List<string> LoadUsersBlacklist()
        {
            return _filesControl.LoadUsersBlacklist();
        }

        internal User LoadUserData(User user)
        {
            return _filesControl.LoadUserData(user);
        }

        internal List<TempUser> GetPointsLeaderboard()
        {
            return _filesControl.GetPointsLeaderboard();
        }

        internal List<TempUser> GetExperienceLeaderboard()
        {
            return _filesControl.GetExperienceLeaderboard();
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
