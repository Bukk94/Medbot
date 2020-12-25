using Medbot.Events;
using System;

namespace Medbot
{
    public interface IBotClient
    {
        event EventHandler<OnCommandReceivedArgs> OnCommandReceived;
        event EventHandler<OnMessageArgs> OnMessageReceived;
        event EventHandler<OnMessageArgs> OnMessageSent;
        event EventHandler<OnUserArgs> OnUserJoined;
        event EventHandler<OnUserArgs> OnUserDisconnected;
        event EventHandler<TimeSpan> OnUptimeTick;
        event EventHandler<OnMessageArgs> OnConsoleOuput;

        bool IsConnectionAlive { get; }
        bool IsBotRunning { get; }
        string DeployedChannel { get; }

        void Start();
        void Stop();
        bool Connect();
        void Disconnect();
        void SendChatMessage(string msg);
        void SendChatMessage(string msg, bool isCommand);
        void SendPrivateMessage(string msg, string user);
    }
}
