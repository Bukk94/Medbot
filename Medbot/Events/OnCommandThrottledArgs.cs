using Medbot.Commands;
using System;

namespace Medbot.Events {
    internal class OnCommandThrottledArgs {

        public Command Command { get; set; }

        public TimeSpan Interval { get; set; }

    }
}
