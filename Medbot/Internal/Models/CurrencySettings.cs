using System;

namespace Medbot.Internal.Models
{
    public class CurrencySettings
    {
        public string Name { get; set; } = "gold";
        public string Plural { get; set; } = "gold";
        public string Units { get; set; } = "g";
        public TimeSpan TickInterval { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan IdleTime { get; set; } = TimeSpan.FromMinutes(5);
        public int PointsPerTick { get; set; } = 1;
        public bool RewardIdles { get; set; }
    }
}
