namespace Medbot.Internal
{
    internal static class BotDictionary
    {
        /// <summary>
        /// Gets/Sets bot's welcome message
        /// </summary>
        internal static string WelcomeMessage { get; set; }

        /// <summary>
        /// Gets/Sets bot's goodbye message
        /// </summary>
        internal static string GoodbyeMessage { get; set; }

        /// <summary>
        /// Gets/Sets message to announce user's new rank
        /// </summary>
        internal static string NewRankMessage { get; set; }

        /// <summary>
        /// Gets/Sets message to announce failing commands loading
        /// </summary>
        internal static string CommandsNotFound { get; set; }

        /// <summary>
        /// Gets/Sets message to announce zero loaded commands
        /// </summary>
        internal static string ZeroCommands { get; set; }

        /// <summary>
        /// Gets/Sets string for 'yes' word
        /// </summary>
        internal static string Yes { get; set; }

        /// <summary>
        /// Gets/Sets string for 'no' word
        /// </summary>
        internal static string No { get; set; }

        // TODO: Move these to different class, this isn't dictionary, it's a settings!
        /// <summary>
        /// Gets/Sets number of top people printed in !leaderboard command
        /// </summary>
        internal static int LeaderboardTopNumber { get; set; }

        /// <summary>
        /// Gets/Sets percentage of winning triple bonus in the gamble
        /// </summary>
        internal static int GambleBonusWinPercentage { get; set; }

        /// <summary>
        /// Gets/Sets percentage of winning the gamble
        /// </summary>
        internal static int GambleWinPercentage { get; set; }
    }
}
