using System;

namespace Medbot.Events
{
    public class OnCommandResponseArgs : EventArgs
    {
        public OnCommandResponseArgs()
        {
        }

        public OnCommandResponseArgs(string message) : this(message, false)
        {
        }

        public OnCommandResponseArgs(string message, bool isCommand)
        {
            this.Message = message;
            this.IsResponseACommand = isCommand;
        }

        public string Message { get; set; }

        public bool IsResponseACommand { get; set; } = false;
    }
}
