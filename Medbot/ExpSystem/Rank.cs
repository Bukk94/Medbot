using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medbot.ExpSystem {
    internal class Rank {
        private string rankName;
        private long expRequired;
        private int rankLevel;

        /// <summary>
        /// Gets a rank name
        /// </summary>
        internal string RankName { get { return this.rankName; } }

        /// <summary>
        /// Gets a rank level
        /// </summary>
        internal int RankLevel { get { return this.rankLevel; } }

        /// <summary>
        /// Gets experience required to gain this level
        /// </summary>
        internal long ExpRequired { get { return this.expRequired; } }

        /// <summary>
        /// Rank structure containing rank name, rank level and experience required to gain this rank
        /// </summary>
        /// <param name="name">Name of the rank</param>
        /// <param name="level">Level of the rank</param>
        /// <param name="exp">Experience required to gain rank</param>
        internal Rank(string name, int level, long exp) {
            this.rankName = name;
            this.rankLevel = level;
            this.expRequired = exp;
        }
    }
}
