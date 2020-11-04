using System;

namespace Medbot.Events
{
    public class OnMessageArgs : EventArgs
    {
        public string Message { get; set; }

        public User Sender { get; set; }
    }
}
