namespace Medbot.Internal.Models
{
    internal class BotSettings
    {
        /// <summary>
        /// Number of top people printed in !leaderboard command
        /// </summary>
        public int LeaderboardTopNumber { get; set; } = 3;

        /// <summary>
        /// Percentage of winning the gamble minigame
        /// </summary>
        public int GambleWinPercentage { get; set; } = 20;

        /// <summary>
        /// Percentage of winning triple bonus in the gamble minigame
        /// </summary>
        public int GambleBonusWinPercentage { get; set; } = 2;

        /// <summary>
        /// Bot will greet people when joined in the channel
        /// Uses DictionaryStrings.WelcomeMessage
        /// </summary>
        public bool GreetOnBotJoining { get; set; }

        /// <summary>
        /// Bot will say it's goodbyes when leaving the channel
        /// Uses DictionaryString.GoodbyeMessage
        /// </summary>
        public bool FarewellOnBotLeaving { get; set; }

        /// <summary>
        /// Default value for using colored messages
        /// This can be turn off anytime using command !colors off/on
        /// </summary>
        public bool UseColoredMessages { get; set; }
    }
}
