using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medbot.Internal {
    internal static class BotDictionary {

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
        /// Gets/Sets string for 'yes' word
        /// </summary>
        internal static string Yes { get; set; }

        /// <summary>
        /// Gets/Sets string for 'no' word
        /// </summary>
        internal static string No { get; set; }

        /// <summary>
        /// Gets/Sets number of top people printed in !leaderboard command
        /// </summary>
        internal static int LeaderboardTopNumber { get; set; }
    }
}
