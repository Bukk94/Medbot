namespace Medbot.ExpSystem
{
    public class Rank
    {
        /// <summary>
        /// Gets a rank name
        /// </summary>
        public string RankName { get; private set; }

        /// <summary>
        /// Gets a rank level
        /// </summary>
        public int RankLevel { get; private set; }

        /// <summary>
        /// Gets experience required to gain this level
        /// </summary>
        public long ExpRequired { get; private set; }

        /// <summary>
        /// Rank structure containing rank name, rank level and experience required to gain this rank
        /// </summary>
        /// <param name="name">Name of the rank</param>
        /// <param name="level">Level of the rank</param>
        /// <param name="exp">Experience required to gain rank</param>
        public Rank(string name, int level, long exp)
        {
            this.RankName = name;
            this.RankLevel = level;
            this.ExpRequired = exp;
        }
    }
}
