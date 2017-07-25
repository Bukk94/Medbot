using Medbot.Commands;
using Medbot.Events;
using System;
using System.Collections.Generic;

namespace Medbot {
    interface IBotClient {
        event EventHandler<OnCommandReceivedArgs> OnCommandReceived;
        event EventHandler<OnMessageArgs> OnMessageReceived;
        event EventHandler<OnMessageArgs> OnMessageSent;
        event EventHandler<OnUserArgs> OnUserJoined;
        event EventHandler<OnUserArgs> OnUserDisconnected;

        List<Command> CommandsList { get; }
        bool IsConnectionAlive { get; }

        void Start();
        void Stop();
        void Connect();
        void Disconnect();
        User GetUserFromChat(string chatLine);
        void SendChatMessage(string msg);
        void SendChatMessage(string msg, bool isCommand);
        void SendPrivateMessage(string msg, string user);
        void SaveData();
    }
}
