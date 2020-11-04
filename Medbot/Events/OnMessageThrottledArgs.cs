using Medbot.Internal;
using System;

namespace Medbot.Events
{
    public class OnMessageThrottledArgs : EventArgs
    {
        public ThrottleViolation Violation { get; set; }

        public string Message { get; set; }

        public TimeSpan Interval { get; set; }
    }
}
