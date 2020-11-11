using Medbot.Users;

namespace Medbot.Internal
{
    internal class BotDataManager
    {
        internal bool IsBotModerator => BotObject?.IsModerator ?? false;

        internal User BotObject { get; set; }

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
