using System;

namespace Medbot.ExpSystem {
    public class Rank {
        private string rankName;
        private long expRequired;
        private int rankLevel;

        /// <summary>
        /// Gets a rank name
        /// </summary>
        public string RankName { get { return this.rankName; } }

        /// <summary>
        /// Gets a rank level
        /// </summary>
        public int RankLevel { get { return this.rankLevel; } }

        /// <summary>
        /// Gets experience required to gain this level
        /// </summary>
        public long ExpRequired { get { return this.expRequired; } }

        /// <summary>
        /// Rank structure containing rank name, rank level and experience required to gain this rank
        /// </summary>
        /// <param name="name">Name of the rank</param>
        /// <param name="level">Level of the rank</param>
        /// <param name="exp">Experience required to gain rank</param>
        public Rank(string name, int level, long exp) {
            this.rankName = name;
            this.rankLevel = level;
            this.expRequired = exp;
        }
    }
}
