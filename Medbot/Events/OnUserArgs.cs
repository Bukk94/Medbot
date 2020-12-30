using Medbot.Users;
using System;

namespace Medbot.Events
{
    public class OnUserArgs : EventArgs
    {
        public User User { get; set; }
    }
}
