using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Medbot.Commands;
using Medbot.ExpSystem;
using Medbot.Internal;
using Medbot.Events;
using System.Threading.Tasks;
using System.Diagnostics;
using Medbot.Points;
using Medbot.Users;
using Medbot.Enums;
using Microsoft.Extensions.Logging;

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
        private readonly CommandsHandler _commandsHandler;
        private readonly MessageThrottling throttler;
        private readonly ILogger _logger;
        private TcpClient tcpClient;
        private StreamWriter writer;
        private StreamReader reader;

        #region Properties
        /// <summary>
        /// Channel on which bot is deployed on
        /// </summary>
        public string DeployedChannel => _botDataManager.Login.Channel;

        /// <summary>
        /// Bool if the bot is running
        /// </summary>
        public bool IsBotRunning { get; private set; }

        /// <summary>
        /// Bool if the connection of the bot is alive
        /// </summary>
        public bool IsConnectionAlive => tcpClient != null ? tcpClient.Connected : false;
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

        public BotClient()
        {
            _logger = Logging.GetLogger<BotClient>();

            _botDataManager = new BotDataManager();
            _usersManager = new UsersManager(_botDataManager);
            throttler = new MessageThrottling(_botDataManager);

            chatMessagePrefix = string.Format(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :", _botDataManager.Login.BotName, _botDataManager.Login.Channel);
            _pointsManager = new PointsManager(_botDataManager, _usersManager, false);

            _experienceManager = new ExperienceManager(_botDataManager, _usersManager, false);
            _commandsHandler = new CommandsHandler(_usersManager, _botDataManager, _experienceManager, _pointsManager);
            _commandsHandler.OnCommandResponse += CommandsHandler_OnCommandResponse;

            this.readingTimer = new Timer(Reader_Timer_Tick, null, Timeout.Infinite, 200);
            this.uptimeTimer = new Timer(Uptime_Timer_Tick, null, Timeout.Infinite, 1000);
            this.uptime = new Stopwatch();

            SetupEvents();
            // Task.Run(() => TestAPI()).Wait();
        }

        public async Task TestAPI()
        {
            var isLive = await IsBroadcasterLive();
            var test = await Requests.TwitchJsonRequestAsync("https://api.twitch.tv/helix/users/follows?to_id=24395849&first=1", RequestType.GET);
            var test2 = await Requests.TwitchJsonRequestAsync("https://api.twitch.tv/helix/streams?user_login=bukk94", RequestType.GET);
        }

        private void SetupEvents()
        {
            _usersManager.OnUserJoined += (sender, e) => OnUserJoined?.Invoke(sender, e);
            _usersManager.OnUserDisconnected += (sender, e) => OnUserDisconnected?.Invoke(sender, e);
            _experienceManager.OnRankUp += ExperienceManager_OnRankUp;
        }

        /// <summary>
        /// Starts the bot. Auto-connects to bot's Twitch account
        /// </summary>
        public void Start()
        {
            if (IsConnectionAlive)
            {
                _logger.LogWarning("Cant start the bot! Bot is already running.");
                return;
            }

            var connected = Connect();
            if (!connected)
                return;

            // Immediatelly start primary timer
            this.readingTimer.Change(0, 200);

            // Start points timer
            if (!_pointsManager.IsTimerRunning)
                _pointsManager.StartPointsTimer();

            // Start experience timer
            if (!_experienceManager.TimerRunning)
                _experienceManager.StartExperienceTimer();

            this.uptime.Start();
            this.uptimeTimer.Change(0, 1000);
            IsBotRunning = true;
            _logger.LogInformation("Bot started.");
        }

        /// <summary>
        /// Stops the bot. Disconnects bot from his Twitch account and discarding TCP connection
        /// </summary>
        public void Stop()
        {
            if (!IsConnectionAlive)
            {
                _logger.LogWarning("Can't stop the bot! Bot is not running.");
                return;
            }

            this.readingTimer.Change(Timeout.Infinite, 200);
            Disconnect();

            // Shutdown points timer
            if (_pointsManager.IsTimerRunning)
                _pointsManager.StopPointsTimer();

            // Shutdown experience timer
            if (_experienceManager.TimerRunning)
                _experienceManager.StopExperienceTimer();

            IsBotRunning = false;
            this.uptime.Stop();
            this.uptimeTimer.Change(Timeout.Infinite, 0);
            _logger.LogInformation("Bot has been stopped.");
        }

        /// <summary>
        /// Connects the bot to the Twitch account
        /// </summary>
        public bool Connect()
        {
            if (!_botDataManager.Login.IsLoginCredentialsValid)
            {
                _logger.LogError("Can't connect the bot - Invalid credentials!");
                return false;
            }

            try
            {
                //var encoding = Encoding.GetEncoding(65001, new EncoderExceptionFallback(), new DecoderReplacementFallback(string.Empty));
                tcpClient = new TcpClient("irc.chat.twitch.tv", 6667);
                reader = new StreamReader(tcpClient.GetStream(), Encoding.UTF8);
                writer = new StreamWriter(tcpClient.GetStream());

                writer.WriteLine("PASS " + _botDataManager.Login.BotOAuthWithPrefix + Environment.NewLine +
                                 "NICK " + _botDataManager.Login.BotName + Environment.NewLine +
                                 "USER " + _botDataManager.Login.BotName + " 8 * :" + _botDataManager.Login.BotName);
                writer.WriteLine("CAP REQ :twitch.tv/membership");
                writer.WriteLine("CAP REQ :twitch.tv/commands");
                writer.WriteLine("CAP REQ :twitch.tv/tags");
                writer.WriteLine("JOIN #" + _botDataManager.Login.Channel);
                writer.Flush();

                if (_botDataManager.BotSettings.GreetOnBotJoining)
                    SendChatMessage(_botDataManager.BotDictionary.WelcomeMessage);

                _logger.LogInformation("Bot has successfully connected to Twitch account and joined the channel {channel}.", _botDataManager.Login.Channel);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured during bot connecting.\n{ex}", ex);
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
                if (_botDataManager.BotSettings.FarewellOnBotLeaving)
                    SendChatMessage(_botDataManager.BotDictionary.GoodbyeMessage);

                _usersManager.SaveData();
                reader.Close();
                writer.Close();
                tcpClient.Close();
                _usersManager.ClearOnlineUsers();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred during bot disconnection.\n{ex}", ex);
            }
        }

        private void ConsoleAppendText(string text)
        {
            _logger.LogInformation(text);
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

                        if (message.ContainsInsensitive("@" + _botDataManager.Login.BotFullTwitchName))
                        { // Bot is called by it's name, respond somehow
                            SendChatMessage(_botDataManager.BotDictionary.BotRespondMessages.SelectOneRandom());
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
                        var botUser = _usersManager.FindOnlineUser(_botDataManager.Login.BotName);
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
                _logger.LogError("Fatal error occured in bot timer mechanism!\n{ex}", ex);
            }
        }

        /// <summary>
        /// Responds to command call
        /// </summary>
        /// <param name="sender">User who sent the command</param>
        /// <param name="message">String command message send by user</param>
        private void RespondToCommand(User sender, string message)
        {
            // TODO: Fix this parsing and make it more efficient
            var command = _commandsHandler.CommandsList.FirstOrDefault(cmd =>
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

                string commandResult = _commandsHandler.ExecuteCommand(command, sender, Parsing.ParseCommandValues(message));
                OnCommandReceived?.Invoke(this, new OnCommandReceivedArgs { Command = command });
                DeliverCommandResults(command, commandResult, sender);
            }
        }

        /// <summary>
        /// Delivers command results to chat or whisper, depending on command's settings
        /// </summary>
        /// <param name="command">Executed command</param>
        /// <param name="commandResultsToSend">Command results</param>
        /// <param name="sender">User who send the command</param>
        private void DeliverCommandResults(Command command, string commandResultsToSend, User sender)
        {
            if (command.SendWhisper)
                SendPrivateMessage(commandResultsToSend, sender.Username);
            else
                SendChatMessage(commandResultsToSend);
        }

        /// <summary>
        /// Gets User from chat message
        /// </summary>
        /// <param name="chatLine">Full chat line</param>
        /// <returns>Sender user</returns>
        private User GetUserFromChat(string chatLine)
        {
            User sender = _usersManager.JoinUser(chatLine);
            _experienceManager.CheckUserRankUp(sender);
            sender.ApplyBadges(Parsing.ParseBadges(chatLine));
            return sender;
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
            if (string.IsNullOrEmpty(msg))
                return;

            // :sender!sender@sender.tmi.twitch.tv PRIVMSG #channel :message
            if (!IsConnectionAlive)
            {
                _logger.LogWarning("Cannot send chat message, connection is NOT alive! Message '{msg}' was not send.", msg);
                return;
            }

            if (!throttler.AllowToSendMessage(msg))
                return;

            writer.WriteLine(String.Format("{0}{1}{2}", chatMessagePrefix, _botDataManager.UseColoredMessages && !isCommand ? "/me " : "", msg));
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
                _logger.LogWarning("Cannot send whisper message, connection is NOT alive! Whisper '{msg}' to {user} was not send.", user, msg);
                return;
            }

            if (!throttler.AllowToSendMessage(msg))
                return;

            // FIX: Private message sending
            // There is problem with sending PMs. Bot can't start conversation first
            writer.WriteLine(String.Format("{0}/w {1} {2}", chatMessagePrefix, user.ToLower(), msg));
            writer.Flush();
            ConsoleAppendText(String.Format("{0}/w {1} {2}", chatMessagePrefix, user.ToLower(), msg));
        }

        private async Task<bool> IsBroadcasterLive()
        {
            // https://api.twitch.tv/helix/streams?user_login=Bukk94
            // https://api.twitch.tv/helix/streams?user_id=5798794897
            var data = await Requests.TwitchJsonRequestAsync($"https://api.twitch.tv/helix/streams?user_login={_botDataManager.Login.Channel}", RequestType.GET);

            return Requests.GetJsonData(data)?.Any() == true;
        }

        private void Uptime_Timer_Tick(Object sender)
        {
            OnUptimeTick?.Invoke(this, uptime.Elapsed);
        }

        private void ExperienceManager_OnRankUp(object sender, OnRankUpArgs e)
        {
            SendChatMessage(String.Format(_botDataManager.BotDictionary.NewRankMessage, e.User.DisplayName, e.NewRank.RankLevel, e.NewRank.RankName));
        }

        private void CommandsHandler_OnCommandResponse(object sender, OnCommandResponseArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
                SendChatMessage(e.Message, e.IsResponseACommand);
        }
    }
}
