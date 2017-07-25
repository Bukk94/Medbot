using System;
using Medbot.Commands;

namespace Medbot.Events {
    internal class OnCommandReceivedArgs : EventArgs {

        internal Command Command { get; set; }

    }
}
