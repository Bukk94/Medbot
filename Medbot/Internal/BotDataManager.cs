using Medbot.Commands;
using Medbot.Users;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Medbot.Internal
{
    internal class BotDataManager
    {
        private readonly FilesControl _filesControl;

        internal bool IsBotModerator => BotObject?.IsModerator ?? false;

        internal User BotObject { get; set; }

        internal Dictionary<string, int> BotIntervals { get; private set; }

        /// <summary>
        /// Full path to settings XML file
        /// </summary>
        public string SettingsPath => Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Settings.xml";

        /// <summary>
        /// Full path to file where are all users data stored
        /// </summary>
        public string DataPath => Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Users_data.xml";

        /// <summary>
        /// Full path to file where are all users data stored
        /// </summary>
        public string CommandsPath => Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Commands.xml";

        /// <summary>
        /// Full path to ranks file
        /// </summary>
        public string RanksPath => Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Ranks.txt";

        public BotDataManager()
        {
            _filesControl = new FilesControl(this);

            _filesControl.LoadLoginCredentials().Wait();
            _filesControl.LoadBotDictionary();
            BotIntervals = _filesControl.LoadBotIntervals();
        }

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

        // TODO: Delete this and create Commands list here
        internal List<Command> LoadCommands()
        {
            return _filesControl.LoadCommands();
        }

        internal List<TempUser> GetPointsLeaderboard()
        {
            return _filesControl.GetPointsLeaderboard();
        }

        internal List<TempUser> GetExperienceLeaderboard()
        {
            return _filesControl.GetExperienceLeaderboard();
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
