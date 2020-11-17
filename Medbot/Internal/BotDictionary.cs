using System;

namespace Medbot.Internal
{
    [Obsolete("Remove this and replace it with BotDataManager.BotDictionary")]
    internal static class BotDictionary
    {
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
