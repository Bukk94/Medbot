using Medbot.ExpSystem;
using Medbot.Users;
using System;

namespace Medbot.Events
{
    public class OnRankUpArgs : EventArgs
    {
        public User User { get; set; }
        public Rank NewRank { get; set; }
    }
}
