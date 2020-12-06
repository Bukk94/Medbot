using System.Collections.Generic;

namespace Medbot.Internal.Models
{
    internal class AllSettings
    {
        public LoginDetails Login { get; set; }
        public BotSettings Settings { get; set; }
        public CurrencySettings Currency { get; set; }
        public ExperienceSettings Experience { get; set; }
        public List<string> Blacklist { get; set; }
    }
}
