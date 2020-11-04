using System;
using Medbot.Commands;

namespace Medbot.Events
{
    public class OnCommandReceivedArgs : EventArgs
    {
        public Command Command { get; set; }
    }
}
