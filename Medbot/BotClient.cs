using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Medbot.Commands;
using Medbot.ExpSystem;
using Medbot.LoggingNS;
using Medbot.Internal;
using Medbot.Events;
using System.Threading.Tasks;
using System.Diagnostics;
using Medbot.Points;
using Medbot.Users;

namespace Medbot
{
    public class BotClient : IBotClient
    {
        private readonly string chatMessagePrefix;
        private readonly Timer readingTimer;
        private readonly Timer uptimeTimer;
        private readonly Stopwatch uptime;
        private readonly PointsManager _pointsManager;
        private readonly ExperienceManager _experienceManager;
        private readonly UsersManager _usersManager;
        private readonly BotDataManager _botDataManager;
        private readonly MessageThrottling throttler;
        private TcpClient tcpClient;
        private StreamWriter writer;
        private StreamReader reader;

        // TODO: Get rid of static properties
        #region Properties
        /// <summary>
        /// Full path to settings XML file
        /// </summary>
        public static string SettingsPath => Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Settings.xml";

        /// <summary>
        /// Full path to file where are all users data stored
        /// </summary>
        public static string DataPath => Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Users_data.xml";

        /// <summary>
        /// Full path to file where are all users data stored
        /// </summary>
        public static string CommandsPath => Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Commands.xml";

        /// <summary>
        /// Full path to ranks file
        /// </summary>
        public static string RanksPath => Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Ranks.txt";

        /// <summary>
        /// Channel on which bot is deployed on
        /// </summary>
        public string DeployedChannel => Login.Channel;

        /// <summary>
        /// Bool if the bot is running
        /// </summary>
        public bool IsBotRunning { get; private set; }

        /// <summary>
        /// Gets bool if the connection of the bot is alive
        /// </summary>
        public bool IsConnectionAlive => tcpClient != null ? tcpClient.Connected : false;

        /// <summary>
        /// Bool if bot can use colored messages
        /// </summary>
        public bool UseColoredMessages { get; set; }

        /// <summary>
        /// List of bot's commands
        /// </summary>
        public List<Command> CommandsList { get; }
        #endregion

        #region Events
        /// <summary>
        /// Activates when command is received
        /// </summary>
        public event EventHandler<OnCommandReceivedArgs> OnCommandReceived;

        /// <summary>
        /// Activates when message is received
        /// </summary>
        public event EventHandler<OnMessageArgs> OnMessageReceived;

        /// <summary>
        /// Activates when message is sent by bot
        /// </summary>
        public event EventHandler<OnMessageArgs> OnMessageSent;

        /// <summary>
        /// Activates when user joines the channel
        /// </summary>
        public event EventHandler<OnUserArgs> OnUserJoined;

        /// <summary>
        /// Activates when user disconnects from the channel
        /// </summary>
        public event EventHandler<OnUserArgs> OnUserDisconnected;

        /// <summary>
        /// Activates when uptime timer ticks
        /// </summary>
        public event EventHandler<TimeSpan> OnUptimeTick;

        public event EventHandler<OnMessageArgs> OnConsoleOuput;
        #endregion

        // TODO: Consider backup uploading
        public BotClient()
        {
            UseColoredMessages = true;

            _usersManager = new UsersManager();
            _botDataManager = new BotDataManager();
            throttler = new MessageThrottling(_botDataManager);

            FilesControl.LoadLoginCredentials();
            FilesControl.LoadUsersBlacklist();
            FilesControl.LoadBotDictionary();

            // TODO: Convert interval strings to enum
            Dictionary<string, int> intervals = FilesControl.LoadBotIntervals();

            chatMessagePrefix = String.Format(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :", Login.BotName, Login.Channel);
            _pointsManager = new PointsManager(_usersManager, new TimeSpan(0, intervals["PointsInterval"], 0), new TimeSpan(0,
                                                intervals["PointsIdleTime"], 0), Convert.ToBoolean(intervals["PointsRewardIdles"]),
                                                intervals["PointsPerTick"], false);

            _experienceManager = new ExperienceManager(this, _usersManager, new TimeSpan(0, intervals["ExperienceInterval"], 0),
                                               intervals["ExperienceActiveExp"],
                                               intervals["ExperienceIdleExp"], new TimeSpan(0, intervals["ExperienceIdleTime"], 0), false);
            CommandsHandler.Initialize(_usersManager, _experienceManager, this);
            FilesControl.Initialize(_usersManager);
            CommandsList = FilesControl.LoadCommands();

            if (CommandsList == null)
                SendChatMessage(BotDictionary.CommandsNotFound);
            else if (CommandsList.Count <= 0)
                SendChatMessage(BotDictionary.ZeroCommands);

            this.readingTimer = new Timer(Reader_Timer_Tick, null, Timeout.Infinite, 200);
            this.uptimeTimer = new Timer(Uptime_Timer_Tick, null, Timeout.Infinite, 1000);
            this.uptime = new Stopwatch();

            SetupEvents();
        }

        private void SetupEvents()
        {
            _usersManager.OnUserJoined += (sender, e) => OnUserJoined?.Invoke(sender, e);
            _usersManager.OnUserDisconnected += (sender, e) => OnUserDisconnected?.Invoke(sender, e);
        }

        /// <summary>
        /// Starts the bot. Auto-connects to bot's Twitch account
        /// </summary>
        public void Start()
        {
            if (IsConnectionAlive)
            {
                Console.WriteLine("Bot is already running");
                return;
            }

            var connected = Connect();
            if (!connected)
                return;

            // Immediatelly start primary timer
            this.readingTimer.Change(0, 200);

            // Start points timer
            if (!_pointsManager.TimerRunning)
                _pointsManager.StartPointsTimer();

            // Start experience timer
            if (!_experienceManager.TimerRunning)
                _experienceManager.StartExperienceTimer();

            this.uptime.Start();
            this.uptimeTimer.Change(0, 1000);
            IsBotRunning = true;
            Console.WriteLine("Bot started");
        }

        /// <summary>
        /// Stops the bot. Disconnects bot from his Twitch account and discarding TCP connection
        /// </summary>
        public void Stop()
        {
            if (!IsConnectionAlive)
            {
                Console.WriteLine("Bot is not running");
                return;
            }

            this.readingTimer.Change(Timeout.Infinite, 200);
            Disconnect();

            // Shutdown points timer
            if (_pointsManager.TimerRunning)
                _pointsManager.StopPointsTimer();

            // Shutdown experience timer
            if (_experienceManager.TimerRunning)
                _experienceManager.StopExperienceTimer();

            IsBotRunning = false;
            this.uptime.Stop();
            this.uptimeTimer.Change(Timeout.Infinite, 0);
            Console.WriteLine("Bot has been stopped");
        }

        /// <summary>
        /// Connects the bot to the Twitch account
        /// </summary>
        public bool Connect()
        {
            if (!Login.IsLoginCredentialsValid)
            {
                Console.Error.WriteLine("Can't connect the bot - Invalid credentials!");
                return false;
            }

            try
            {
                //var encoding = Encoding.GetEncoding(65001, new EncoderExceptionFallback(), new DecoderReplacementFallback(string.Empty));
                tcpClient = new TcpClient("irc.chat.twitch.tv", 6667);
                reader = new StreamReader(tcpClient.GetStream(), Encoding.UTF8);
                writer = new StreamWriter(tcpClient.GetStream());

                writer.WriteLine("PASS " + Login.BotOauthWithPrefix + Environment.NewLine +
                                 "NICK " + Login.BotName + Environment.NewLine +
                                 "USER " + Login.BotName + " 8 * :" + Login.BotName);
                writer.WriteLine("CAP REQ :twitch.tv/membership");
                writer.WriteLine("CAP REQ :twitch.tv/commands");
                writer.WriteLine("CAP REQ :twitch.tv/tags");
                writer.WriteLine("JOIN #" + Login.Channel);
                writer.Flush();

                // TODO: Make welcome message optional
                //SendChatMessage(BotDictionary.WelcomeMessage);
                Logging.LogEvent(MethodBase.GetCurrentMethod(), "Bot has successfully connected to Twitch account and joined the channel " + Login.Channel);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured during connecting");
                Console.WriteLine(ex);
                Logging.LogError(this, MethodBase.GetCurrentMethod(), "Error occured during connecting: " + ex.ToString());
            }

            return false;
        }

        /// <summary>
        /// Disconnects the bot from the Twitch account, clean OnlineUsers list and closes TCP connection
        /// </summary>
        public void Disconnect()
        {
            if (!IsBotRunning)
                return;

            try
            {
                // TODO: Make goodbye message optional
                // SendChatMessage(BotDictionary.GoodbyeMessage);

                FilesControl.SaveData();
                reader.Close();
                writer.Close();
                tcpClient.Close();
                _usersManager.ClearOnlineUsers();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void ConsoleAppendText(string text)
        {
            Console.WriteLine(text);
            OnConsoleOuput?.Invoke(this, new OnMessageArgs { Message = text });
        }

        /// <summary>
        /// Main bot's 'heart'. Timer tick event
        /// </summary>
        private void Reader_Timer_Tick(Object s)
        {
            if (!IsConnectionAlive)
                Connect();

            // TODO: Bot should be initialized to online users as fast as possible
            // Current JOIN event is quite slow and can result into bot's insufficient permissions
            try
            {
                if (tcpClient.Available > 0 || reader.Peek() >= 0)
                {
                    string chatLine = reader.ReadLine();
                    ConsoleAppendText(chatLine);

                    // User sent a message
                    if (chatLine.Contains("PRIVMSG"))
                    {
                        // Get user, add him in OnlineUsers
                        User sender = GetUserFromChat(chatLine);

                        // Check if user has Display Name
                        CheckUserDisplayName(sender, chatLine);

                        sender.LastMessage = DateTime.Now;

                        // Parse chat message
                        string message = Parsing.ParseChatMessage(chatLine);
                        OnMessageReceived?.Invoke(this, new OnMessageArgs { Message = message, Sender = sender });

                        if (message.Contains("@" + Login.BotFullTwitchName))
                        { // Bot is called by it's name, respond somehow
                            // TODO: Bot respond
                            SendChatMessage("Ahoj! Jsem MedBot, nový medvědí bot-pomocník. Momentálně jsem ještě ve vývoji, buďte na mě hodný :)");
                        }
                        else if (message.StartsWith("!"))
                        { // Someone is trying to call an command
                            RespondToCommand(sender, message);
                        }
                    }
                    else if (chatLine.Contains("JOIN"))
                    {
                        // User joined, add him in OnlineUsers, apply user's badges
                        GetUserFromChat(chatLine);
                    }
                    else if (chatLine.Contains("PART"))
                    {
                        // User disconnected
                        _usersManager.DisconnectUser(Parsing.ParseUsername(chatLine));
                    }
                    else if (chatLine.Contains("USERSTATE"))
                    {
                        // TODO: What this does?!
                        GetUserFromChat(chatLine);
                        var botUser = _usersManager.FindOnlineUser(Login.BotName);
                        _botDataManager.UpdateBotPermissions(botUser);
                    }
                    else if (chatLine.Contains("PING :tmi.twitch.tv"))
                    {
                        // Request bot response on ping command, keeps connection alive
                        writer.WriteLine("PONG :tmi.twitch.tv");
                        writer.Flush();
                        ConsoleAppendText("PONG :tmi.twitch.tv");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(this, MethodBase.GetCurrentMethod(), ex.ToString());
            }
        }

        /// <summary>
        /// Responds to command call
        /// </summary>
        /// <param name="sender">User who sent the command</param>
        /// <param name="message">String command message send by user</param>
        private void RespondToCommand(User sender, string message)
        {
            var command = CommandsList.FirstOrDefault(cmd =>
            {
                List<string> parsedMessageCommand = message.Split(' ').ToList();
                string messageCmdName = parsedMessageCommand.FirstOrDefault();
                int commandArgs = cmd.CommandFormat.Split(' ').Count();

                return cmd.CommandFormat.Contains(messageCmdName) && parsedMessageCommand.Count == commandArgs;
            });

            if (command != null)
            {
                if (!command.VerifyFormat(message))
                { // Check permission, if user doesn't have permission, don't send anything
                    // FIX: Inform user about wrong command structure?
                    //DeliverCommandResults(command, command.CheckCommandPermissions(sender) ? command.GetAboutInfoMessage() : "", sender);
                    return;
                }

                string commandResult = command.Execute(sender, Parsing.ParseCommandValues(message));
                OnCommandReceived?.Invoke(this, new OnCommandReceivedArgs { Command = command });
                DeliverCommandResults(command, commandResult, sender);
            }
        }

        /// <summary>
        /// Delivers command results to chat or whisper, depending on command's settings
        /// </summary>
        /// <param name="cmd">Executed command</param>
        /// <param name="result">Command results</param>
        /// <param name="sender">User who send the command</param>
        private void DeliverCommandResults(Command cmd, string result, User sender)
        {
            if (cmd.SendWhisper)
                SendPrivateMessage(result, sender.Username);
            else
                SendChatMessage(result);
        }

        /// <summary>
        /// Gets User from chat message
        /// </summary>
        /// <param name="chatLine">Full chat line</param>
        /// <returns>Sender user</returns>
        private User GetUserFromChat(string chatLine)
        {
            User sender = _usersManager.JoinUser(chatLine);
            ApplyUserBadges(sender, Parsing.ParseBadges(chatLine));
            return sender;
        }

        /// <summary>
        /// Applies user badges to User's object
        /// </summary>
        /// <param name="user">User which should be given the badges</param>
        /// <param name="badges">List of badges</param>
        private void ApplyUserBadges(User user, List<string> badges)
        {
            user.ApplyBadges(badges);
        }

        /// <summary>
        /// Checks if User's object has Display Name filled. If not, it will parse and set from PRIVMSG 
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="chatLine">PRIVMSG chat line containing Display name</param>
        private void CheckUserDisplayName(User user, string chatLine)
        {
            if (user.Username.Equals(user.DisplayName))
            {
                string displayName = Parsing.ParseDisplayName(chatLine);
                if (!String.IsNullOrEmpty(displayName))
                    user.DisplayName = displayName;
            }
        }

        /// <summary>
        /// Sends chat message via bot
        /// </summary>
        /// <param name="msg">String message to send</param>
        public void SendChatMessage(string msg)
        {
            SendChatMessage(msg, false);
        }

        /// <summary>
        /// Sends chat message via bot
        /// </summary>
        /// <param name="msg">String message to send</param>
        public void SendChatMessage(string msg, bool isCommand)
        {
            // :sender!sender@sender.tmi.twitch.tv PRIVMSG #channel :message
            if (!IsConnectionAlive)
            {
                Console.WriteLine("Cannot send chat message, connection is NOT alive");
                return;
            }
            if (!throttler.AllowToSendMessage(msg))
                return;

            writer.WriteLine(String.Format("{0}{1}{2}", chatMessagePrefix, UseColoredMessages && !isCommand ? "/me " : "", msg));
            writer.Flush();
            ConsoleAppendText(chatMessagePrefix + msg);
            OnMessageSent?.Invoke(this, new OnMessageArgs { Message = msg });
        }

        /// <summary>
        /// Sends whisper to user via bot
        /// </summary>
        /// <param name="msg">String whisp message to send</param>
        /// <param name="user">User where whisper should be send</param>
        public void SendPrivateMessage(string msg, string user)
        {
            // :sender!sender@sender.tmi.twitch.tv PRIVMSG #channel :/w user message
            // User must be present in the chat room! Otherwise whisp won't be sent
            if (!IsConnectionAlive)
            {
                Console.WriteLine("Cannot send whisp message, connection is NOT alive");
                return;
            }
            if (!throttler.AllowToSendMessage(msg))
                return;

            // FIX: Private message sending
            // There is problem with sending PMs. Bot can't start conversation first
            writer.WriteLine(String.Format("{0}/w {1} {2}", chatMessagePrefix, user.ToLower(), msg));
            writer.Flush();
            ConsoleAppendText(String.Format("{0}/w {1} {2}", chatMessagePrefix, user.ToLower(), msg));
            Console.WriteLine("Private message sent");
        }

        /// <summary>
        /// Saves all online users points
        /// </summary>
        public void SaveData()
        {
            FilesControl.SaveData();
        }

        private void Uptime_Timer_Tick(Object sender)
        {
            OnUptimeTick?.Invoke(this, uptime.Elapsed);
        }
    }
}
