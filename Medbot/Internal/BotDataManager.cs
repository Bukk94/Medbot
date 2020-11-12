using Medbot.Users;
using System.IO;

namespace Medbot.Internal
{
    internal class BotDataManager
    {
        internal bool IsBotModerator => BotObject?.IsModerator ?? false;

        internal User BotObject { get; set; }

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
