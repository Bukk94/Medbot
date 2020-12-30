using Medbot.Users;
using System.Collections.Generic;

namespace Medbot.Internal
{
    internal class LeaderboardComparer : IComparer<TempUser>
    {
        public int Compare(TempUser x, TempUser y)
        {
            long longX = long.Parse(x.Data);
            long longY = long.Parse(y.Data);

            return longX.CompareTo(longY);
        }
    }
}
