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
    }
}
