using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Medbot.LoggingNS;
using Medbot.Followers;

namespace Medbot.Commands {
    internal static class CommandsHandler {
        private static string commandsFilePath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Commands.xml";
        private static Points pointsObject;
        // TODO: !leaderboard (top 3 xp, top 3 pointu)
        // TODO: !leaderboard med/xp (vypise kdo ma aktualne nejvic medu)
        // TODO: !tradehoney {0: pocet} {1:target}

        /// <summary>
        /// Initializes Commands Handler, passing points and XP objects
        /// </summary>
        /// <param name="points">Object of Points class</param>
        // TODO: XP reference
        internal static void Initialize(Points points) {
            pointsObject = points;
        }

        internal static List<Command> LoadCommands() {
            if (!File.Exists(commandsFilePath)) {
                Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), "FAILED to load commands. File not found");
                return null;
            }

            List<Command> commandsList = new List<Command>();
            try {
                XDocument data = XDocument.Load(commandsFilePath);
                var commandsTypeGroups = data.Element("Medbot").Elements("Commands");

                foreach (var cmdGroup in commandsTypeGroups) {
                    CommandType cmdType = (CommandType)Enum.Parse(typeof(CommandType), cmdGroup.Attribute("Type").Value);
                    var commands = cmdGroup.Elements("Command");

                    foreach (var cmd in commands) {
                        HandlerType handler; 
                        if(!Enum.TryParse<HandlerType>(cmd.Attribute("Handler").Value, out handler))
                            continue;

                        bool broadcasterOnly = Parsing.ParseBooleanFromAttribute(cmd, "BroadcasterOnly");

                        Command newCmd = new Command(cmdType, handler, cmd.Value.ToLower(), 
                                                     cmd.Attribute("AboutCommand").Value, 
                                                     cmd.Attribute("SuccessMessage").Value, 
                                                     cmd.Attribute("FailMessage").Value,
                                                     broadcasterOnly,
                                                     broadcasterOnly ? false : Parsing.ParseBooleanFromAttribute(cmd, "ModPermissionRequired"),
                                                     Parsing.ParseBooleanFromAttribute(cmd, "SendWhisper")
                                                     );
                        commandsList.Add(newCmd);
                    }
                }

                Logging.LogEvent(System.Reflection.MethodBase.GetCurrentMethod(), "Commands Loaded");
            } catch (Exception ex) {
                Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                return null;
            }

            return commandsList;
        }

        /// <summary>
        /// Decides which handler should operate the command
        /// </summary>
        /// <param name="methodType">Method type</param>
        /// <param name="cmd">Command to execute</param>
        /// <param name="sender">User who sent the command</param>
        /// <param name="args">Command arguments</param>
        /// <returns>String result</returns>
        public static string ExecuteMethod(CommandType methodType, Command cmd, User sender, List<string> args) {
            string result = String.Empty;

            switch (methodType) {
                case CommandType.Points:
                    result = PointsHandler(sender, cmd, args);
                    break;
                case CommandType.XP:
                    result = ExperienceHandler(sender, cmd, args);
                    break;
                case CommandType.Internal:
                    result = InternalHandler(sender, cmd, args);
                    break;
                default:
                    result = "";
                    break;
            }

            return result;
        }

        /// <summary>
        /// Method handling Points command distribution
        /// </summary>
        /// <param name="sender">User who sent the command</param>
        /// <param name="cmd">Command to execute</param>
        /// <param name="args">Command arguments</param>
        /// <returns>String result</returns>
        private static string PointsHandler(User sender, Command cmd, List<string> args) {
            switch (cmd.CommandHandlerType) {
                case HandlerType.Add:
                    // Add points,  !addhoney 50 Bukk94 |  2 input args {0:Number of points} {1:User to which add points}
                    
                    User receiver = BotClient.FindOnlineUser(args[1]);
                    try {
                        if (receiver == null) { // user is not online
                            pointsObject.AddUserPointsToFile(args[1], long.Parse(args[0]));
                        } else {
                            receiver.AddPoints(long.Parse(args[0]));
                            pointsObject.SavePoints();
                        }
                        Logging.LogEvent(System.Reflection.MethodBase.GetCurrentMethod(), 
                                                 String.Format("{0}  Args: {1}, {2} - Points successfully added", cmd.CommandFormat, args[0], args[1]));

                        // Success: 4 params: {0:User} {1:Number of points} {2:Currency plural} {3:Currency units}
                        return String.Format(cmd.SuccessMessage, args[1], args[0], Points.CurrencyNamePlural, Points.CurrencyUnits);
                    } catch (Exception ex) {
                        Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());

                        // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which add points} 
                        return String.Format(cmd.FailMessage, Points.CurrencyName, Points.CurrencyNamePlural, Points.CurrencyUnits, args[0], receiver != null ? receiver.DisplayName : args[1]);
                    }

                case HandlerType.Remove:
                    //_Remove points, !removehoney 50 Bukk94 |  2 input args {0:Number of points} {1:User to which remove points}
                    User targetUser = BotClient.FindOnlineUser(args[1]);
                    try {
                        if (targetUser == null) { // user is not online
                            pointsObject.RemoveUserPointsFromFile(args[1], long.Parse(args[0]));
                        } else {
                            targetUser.RemovePoints(long.Parse(args[0]));
                            pointsObject.SavePoints();
                        }

                        Logging.LogEvent(System.Reflection.MethodBase.GetCurrentMethod(), 
                                                 String.Format("{0}  Args: {1}, {2} - Points successfully removed", cmd.CommandFormat, args[0], args[1]));

                        // Success: 4 params: {0:User} {1:Number of points} {2:Currency plural} {3:Currency units}
                        return String.Format(cmd.SuccessMessage, args[1], args[0], Points.CurrencyNamePlural, Points.CurrencyUnits);
                    } catch (Exception ex) {
                        Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());

                        // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which remove points} 
                        return String.Format(cmd.FailMessage, Points.CurrencyName, Points.CurrencyNamePlural, Points.CurrencyUnits, args[0], targetUser != null ? targetUser.DisplayName : args[1]);
                    }

                case HandlerType.Info:
                    // Get points info !med |  0 input args
                    if (sender == null) { // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which add points}
                        Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), String.Format("{0} - Sender is null", cmd.CommandFormat));
                        return String.Format(cmd.FailMessage, Points.CurrencyName, Points.CurrencyNamePlural, Points.CurrencyUnits, "N/A", "N/A");
                    }

                    Logging.LogEvent(System.Reflection.MethodBase.GetCurrentMethod(),
                                                 String.Format("{0}  Args: None # Points printed for user {1} # Value: {2}", cmd.CommandFormat, sender.DisplayName, sender.Points));

                    // Success: 4 params: {0:User} {1:Total} {2:Currency plural} {3:Currency units}
                    return String.Format(cmd.SuccessMessage, sender.DisplayName, sender.Points, Points.CurrencyNamePlural, Points.CurrencyUnits); 
            }

            return "Unknown Points handler";
        }

        private static string ExperienceHandler(User sender, Command cmd, List<string> args) {
            // TODO: Experience Handler
            return "";
        }

        /// <summary>
        /// Method handling internal command distribution
        /// </summary>
        /// <param name="sender">User who sent the command</param>
        /// <param name="cmd">Command to execute</param>
        /// <param name="args">Command arguments</param>
        /// <returns>String result</returns>
        private static string InternalHandler(User sender, Command cmd, List<string> args) {
            switch (cmd.CommandHandlerType) {
                case HandlerType.LastFollower:
                    // Last follower, !lastfollower |  0 input args
                    try {
                        Follow last = FollowersClass.GetNewestFollower(Login.Channel, Login.ClientID).Result;
                        if (last == null) // Fail: 0 params
                            throw new Exception("Last follower has not been found");

                        // Success: 3 params: {0:User} {1:Date} {2:Notification (bool)}
                        return String.Format(cmd.SuccessMessage, last.Follower.DisplayName, last.CreatedAt.ToShortDateString(), last.Notifications.ToString().ToLower());
                    } catch (Exception ex) {
                        Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                        return cmd.FailMessage;
                    }

                case HandlerType.Random:
                    // Selects random user,  !random   !random {0}  | 0 or 1 input args {0: Active past X minutes}
                    try {
                        User randomUser = null;
                        int totalUsers = 0;
                        if (args.Count == 0) {
                            // Regular random
                            randomUser = SelectRandomUser(BotClient.OnlineUsers);
                            totalUsers = BotClient.OnlineUsers.Count;
                        } else {
                            // Active users random
                            if (args.Count <= 0)
                                throw new Exception("Command " + cmd.CommandFormat + "should contain some arguments. Found " + args.Count);

                            List<User> users = BotClient.OnlineUsers.Where(u =>
                                                            u.LastMessage != null &&
                                                            DateTime.Now - u.LastMessage < TimeSpan.FromMinutes(int.Parse(args[0]))).ToList();
                            totalUsers = users.Count;
                            randomUser = SelectRandomUser(users);
                        }

                        if (randomUser == null)
                            throw new Exception("No available users to select a random from.");

                        // Success: 1 param: {0:User} {1:number of people to draw}
                        return String.Format(cmd.SuccessMessage, randomUser.DisplayName, totalUsers);
                    } catch (Exception ex) {
                        Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                        return cmd.FailMessage;
                    }
                    break;
            }

            return "Unknown internal handler";
        }

        /// <summary>
        /// Selects random user from the list
        /// </summary>
        /// <param name="usersList">List of users</param>
        /// <returns>Returns random user</returns>
        private static User SelectRandomUser(List<User> usersList) {
            if (usersList.Count <= 0)
                return null;
            else if (usersList.Count == 1)
                return usersList[0];

            Random rand = new Random(Guid.NewGuid().GetHashCode());
            int random = rand.Next(0, usersList.Count);
            return usersList[random];
        }
    }
}
