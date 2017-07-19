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

namespace Medbot {
    internal class BotClient :IBotClient {
        private static bool botMod;
        private static List<User> onlineUsers;
        private readonly string chatMessagePrefix;
        private bool useColor;
        private TcpClient tcpClient;
        private StreamWriter writer;
        private StreamReader reader;
        private Timer readingTimer;
        private Points points;
        private Experiences experience;
        private List<Command> commands;
        private MessageThrottling throttler;

        // REMOVE: After compiling API
        private MainFrame main;
        
        /// <summary>
        /// Gets list of currently online users
        /// </summary>
        public static List<User> OnlineUsers { get { return onlineUsers; } }

        /// <summary>
        /// Gets full path to settings XML file
        /// </summary>
        public static string SettingsPath { get { return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Settings.xml"; } }

        /// <summary>
        /// Gets full path to file where are all users data stored
        /// </summary>
        public static string DataPath { get { return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Users_data.xml"; } }

        /// <summary>
        /// Gets bool if bot has moderator permissions
        /// </summary>
        public static bool BotModeratorPermission { get { return botMod; } }

        /// <summary>
        /// Gets bool if the connection of the bot is alive
        /// </summary>
        public bool IsConnectionAlive { get { return tcpClient != null ? tcpClient.Connected : false; } }

        /// <summary>
        /// Gets/Sets bool if bot can use colored messages
        /// </summary>
        public bool UseColoredMessages { get { return this.useColor; } set { this.useColor = value; } }

        /// <summary>
        /// Gets list of bot's commands
        /// </summary>
        public List<Command> CommandsList { get { return this.commands; } }

        // TODO: Events
        // TODO: Consider backup uploading
        public BotClient(MainFrame main) {
            // REMOVE: After compiling API
            this.main = main;
            /////////////////

            useColor = true;
            botMod = false;
            throttler = new MessageThrottling();

            chatMessagePrefix = String.Format(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :", Login.BotName, Login.Channel);
            onlineUsers = new List<User>();

            FileControl.LoadBotDictionary();
            Dictionary<string, int> intervals = FileControl.LoadBotIntervals();

            points = new Points(new TimeSpan(0, intervals["PointsInterval"], 0), new TimeSpan(0, 
                                                intervals["PointsIdleTime"], 0), Convert.ToBoolean(intervals["PointsRewardIdles"]), 
                                                intervals["PointsPerTick"], true);

            experience = new Experiences(this, new TimeSpan(0, intervals["ExperienceInterval"], 0), 
                                               intervals["ExperienceActiveExp"],
                                               intervals["ExperienceIdleExp"], new TimeSpan(0, intervals["ExperienceIdleTime"], 0), true);
            CommandsHandler.Initialize(experience, this);
            commands = CommandsHandler.LoadCommands();

            if (commands == null)
                SendChatMessage("Nedokázal jsem načíst příkazy. Někdo něco posral. Já za to nemůžu :(");
            else if (commands.Count <= 0)
                SendChatMessage("Něco je špatně ... Nemám tu žádné příkazy se kterými můžu pracovat. Zavolejte pomoc! Je to i ve vašem zájmu! Když je nemám já, nemá je nikdo!");

            
            this.readingTimer = new Timer(Reader_Timer_Tick, null, Timeout.Infinite, 200);
        }

        /// <summary>
        /// Starts the bot. Auto-connects to bot's Twitch account
        /// </summary>
        public void Start() {
            if (IsConnectionAlive) {
                Console.WriteLine("Bot is already running");
                return;
            }

            Connect();
            // Immediatelly start primary timer
            this.readingTimer.Change(0, 200);

            // Start points timer
            if(!points.TimerRunning)
                points.StartPointsTimer();

            // Start experience timer
            if (!experience.TimerRunning)
                experience.StartExperienceTimer();

            Console.WriteLine("Bot started");
        }

        /// <summary>
        /// Stops the bot. Disconnects bot from his Twitch account and discarding TCP connection
        /// </summary>
        public void Stop() {
            if (!IsConnectionAlive) {
                Console.WriteLine("Bot is not running");
                return;
            }

            this.readingTimer.Change(Timeout.Infinite, 200);
            Disconnect();

            // Shutdown points timer
            if(points.TimerRunning)
                points.StopPointsTimer();

            // Shutdown experience timer
            if (experience.TimerRunning)
                experience.StopExperienceTimer();
            Console.WriteLine("Bot has been stopped");
        }

        /// <summary>
        /// Connects the bot to the Twitch account
        /// </summary>
        public void Connect() {
            try {
                var encoding = Encoding.GetEncoding(65001, new EncoderExceptionFallback(), new DecoderReplacementFallback(string.Empty));
                tcpClient = new TcpClient("irc.chat.twitch.tv", 6667);
                reader = new StreamReader(tcpClient.GetStream(), encoding);
                writer = new StreamWriter(tcpClient.GetStream());

                writer.WriteLine("PASS " + Login.BotPassword + Environment.NewLine +
                                 "NICK " + Login.BotName + Environment.NewLine +
                                 "USER " + Login.BotName + " 8 * :" + Login.BotName);
                writer.WriteLine("CAP REQ :twitch.tv/membership");
                writer.WriteLine("CAP REQ :twitch.tv/commands");
                writer.WriteLine("CAP REQ :twitch.tv/tags");
                writer.WriteLine("JOIN #" + Login.Channel);
                writer.Flush();

                SendChatMessage(BotDictionary.WelcomeMessage);
                Logging.LogEvent(MethodBase.GetCurrentMethod(), "Bot has successfully connected to Twitch account and joined the channel " + Login.Channel);
            } catch (Exception ex) {
                Console.WriteLine("Error occured during connecting");
                Console.WriteLine(ex);
                Logging.LogError(this, MethodBase.GetCurrentMethod(), "Error occured during connecting: " + ex.ToString());
            }
        }

        /// <summary>
        /// Disconnects the bot from the Twitch account, clean OnlineUsers list and closes TCP connection
        /// </summary>
        public void Disconnect() {
            try {
                SendChatMessage(BotDictionary.GoodbyeMessage);

                FileControl.SaveData();
                reader.Close();
                writer.Close();
                tcpClient.Close();
                OnlineUsers.Clear();
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        // TODO: Change this to console output after compiling API
        private void ConsoleAppendText(string text) {
            main.ConsoleAppendText(text);
        }

        /// <summary>
        /// Main bot's 'hearth'. Timer tick event
        /// </summary>
        private void Reader_Timer_Tick(Object s) {
            if (!IsConnectionAlive)
                Connect();

            try {
                if (tcpClient.Available > 0 || reader.Peek() >= 0) {
                    string chatLine = reader.ReadLine();
                    ConsoleAppendText(chatLine);

                    // User sent a message
                    if (chatLine.Contains("PRIVMSG")) {

                        // Get user, add him in OnlineUsers
                        User sender = GetUserFromChat(chatLine);

                        // Check if user has Display Name
                        CheckUserDisplayName(sender, chatLine);

                        sender.LastMessage = DateTime.Now;

                        // Parse chat message
                        string message = Parsing.ParseChatMessage(chatLine);
                        if (message.Contains("@" + Login.BotFullTwitchName)) { // Bot is called by it's name, respond somehow
                            // TODO: Bot respond
                            SendChatMessage("Ahoj! Jsem MedBot, nový medvědí bot-pomocník. Momentálně jsem ještě ve vývoji, buďte na mě hodný :)");
                        } else if (message.StartsWith("!")) { // Someone is trying to call an command
                            RespondToCommand(sender, message);
                        }
                    } else if (chatLine.Contains("JOIN")) {
                        // User joined, add him in OnlineUsers, apply user's badges
                        GetUserFromChat(chatLine);
                    } else if (chatLine.Contains("PART")) {
                        // User disconnected
                        UserDisconnect(Parsing.ParseUsername(chatLine));
                    } else if (chatLine.Contains("USERSTATE")){
                        GetUserFromChat(chatLine);

                        // FIX: What if someone removes bot's mod permission. How to detect it?
                        CheckBotsPermissions(FindOnlineUser(Login.BotName));
                    } else if (chatLine.Contains("PING :tmi.twitch.tv")) {
                        // Request bot response on ping command, keep connection alive
                        writer.WriteLine("PONG :tmi.twitch.tv");
                        writer.Flush();
                        ConsoleAppendText("PONG :tmi.twitch.tv");
                    }
                }
            } catch (Exception ex) {
                Logging.LogError(this, MethodBase.GetCurrentMethod(), ex.ToString());
            }
        }

        /// <summary>
        /// Responds to command call
        /// </summary>
        /// <param name="sender">User who sent the command</param>
        /// <param name="message">String command message send by user</param>
        private void RespondToCommand(User sender, string message) {
            var command = commands.FirstOrDefault(cmd => {
                                                  List<string> parsedMessageCommand = message.Split(' ').ToList();
                                                  string messageCmdName = parsedMessageCommand.FirstOrDefault();
                                                  int commandArgs = cmd.CommandFormat.Split(' ').Count();

                                                  return cmd.CommandFormat.Contains(messageCmdName) && parsedMessageCommand.Count == commandArgs; });

            if (command != null) {
                if (!command.VerifyFormat(message)) { // Check permission, if user doesn't have permission, don't send anything
                    DeliverCommandResults(command, command.CheckCommandPermissions(sender) ? command.GetAboutInfoMessage() : "", sender);
                    return;
                }

                string commandResult = command.Execute(sender, Parsing.ParseCommandValues(message));
                DeliverCommandResults(command, commandResult, sender);
            }
        }

        /// <summary>
        /// Delivers command results to chat or whisper, depending on command's settings
        /// </summary>
        /// <param name="cmd">Executed command</param>
        /// <param name="result">Command results</param>
        /// <param name="sender">User who send the command</param>
        private void DeliverCommandResults(Command cmd, string result, User sender) {
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
        public User GetUserFromChat(string chatLine) {
            User sender = TryUserJoin(chatLine);
            ApplyUserBadges(sender, Parsing.ParseBadges(chatLine));
            return sender;
        }

        /// <summary>
        /// Tries to add user to Online List, if already exists, find him and return
        /// </summary>
        /// <param name="chatLine">Full chat line</param>
        /// <returns>Sender user</returns>
        private User TryUserJoin(string chatLine) {
            string user = Parsing.ParseUsername(chatLine);

            if (!onlineUsers.Any(u => u.Username == user)) { // User is not in the list
                User newUser = new User(user);

                FileControl.LoadUserData(ref newUser);
                onlineUsers.Add(newUser);

                Console.WriteLine("User " + user + " JOINED");
                return newUser;
            }

            return onlineUsers.Find(u => u.Username.Equals(user));
        }

        /// <summary>
        /// Applies user badges to User's object
        /// </summary>
        /// <param name="user">User which should be given the badges</param>
        /// <param name="badges">List of badges</param>
        private void ApplyUserBadges(User user, List<string> badges) {
            user.ApplyBadges(badges);
        }

        /// <summary>
        /// Checks if User's object has Display Name filled. If not, it will parse and set from PRIVMSG 
        /// </summary>
        /// <param name="user">User to check</param>
        /// <param name="chatLine">PRIVMSG chat line containing Display name</param>
        private void CheckUserDisplayName(User user, string chatLine) {
            if (user.Username.Equals(user.DisplayName)) {
                string displayName = Parsing.ParseDisplayName(chatLine);
                if (!String.IsNullOrEmpty(displayName))
                    user.DisplayName = displayName;
            }
        }

        /// <summary>
        /// User disconnected, remove from OnlineUsers and save all points
        /// </summary>
        /// <param name="user"></param>
        private void UserDisconnect(string user) {
            FileControl.SaveData();
            onlineUsers.RemoveAll(u => u.Username == user);
            Console.WriteLine("User " + user + " LEFT");
        }

        /// <summary>
        /// Finds user from Online Users list by its name
        /// </summary>
        /// <param name="username">Username to find</param>
        /// <returns>User object or null</returns>
        public static User FindOnlineUser(string username) {
            return OnlineUsers.Find(u => u.Username.Equals(username.ToLower()));
        }

        /// <summary>
        /// Sends chat message via bot
        /// </summary>
        /// <param name="msg">String message to send</param>
        public void SendChatMessage(string msg) {
            // :sender!sender@sender.tmi.twitch.tv PRIVMSG #channel :message
            if (!IsConnectionAlive) {
                Console.WriteLine("Cannot send chat message, connection is NOT alive");
                return;
            }
            if (!throttler.AllowToSendMessage(msg))
                return;

            writer.WriteLine(String.Format("{0}{1}{2}", chatMessagePrefix, useColor ? "/me " : "", msg));
            writer.Flush();
            ConsoleAppendText(chatMessagePrefix + msg);
        }

        /// <summary>
        /// Sends whisper to user via bot
        /// </summary>
        /// <param name="msg">String whisp message to send</param>
        /// <param name="user">User where whisper should be send</param>
        public void SendPrivateMessage(string msg, string user) {
            // :sender!sender@sender.tmi.twitch.tv PRIVMSG #channel :/w user message
            // User must be present in the chat room! Otherwise whisp won't be sent
            if (!IsConnectionAlive) {
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
        public void SaveData() {
            FileControl.SaveData();
        }

        /// <summary>
        /// Updates class's static variable BotModeratorPermission
        /// </summary>
        /// <param name="botObject">Bot's user object</param>
        private void CheckBotsPermissions(User botObject) {
            if (botObject == null || botObject.Moderator == botMod) // Do nothing if botObject is null or permissions were not changed
                return;
            
            botMod = botObject.Moderator;
        }
    }
}
