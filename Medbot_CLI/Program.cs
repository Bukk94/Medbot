using Medbot;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Medbot_CLI
{
    class Program
    {
        private static IBotClient _botClient;

        static void Main(string[] args)
        {
            // React to close window event, CTRL-C, kill
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            Start();

            // Hold the console so it doesn’t run off the end
            while (!exitSystem)
            {
                Thread.Sleep(500);
            }
        }

        public static void Start()
        {
            _botClient = new BotClient();
            _botClient.Start();
        }

        #region Trap application termination
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;
        static bool exitSystem = false;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            _botClient?.Disconnect();

            // Allow main to run off
            exitSystem = true;

            // Shutdown right away so there are no lingering threads
            Environment.Exit(-1);

            return true;
        }
        #endregion
    }
}
