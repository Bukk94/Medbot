using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medbot {
    interface IBotClient {
        void Start();
        void Connect();
        User GetUserFromChat(string chatLine);
        void SendChatMessage(string msg);
        void SendPrivateMessage(string msg, string user);

    }
}
