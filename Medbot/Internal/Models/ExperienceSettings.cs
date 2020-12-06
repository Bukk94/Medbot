using System;

namespace Medbot.Internal.Models
{
    public class ExperienceSettings
    {
        public TimeSpan TickInterval { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan IdleTime { get; set; } = TimeSpan.FromMinutes(5);
        public int ActiveExp { get; set; } = 5;
        public int IdleExp { get; set; } = 1;
    }
}
