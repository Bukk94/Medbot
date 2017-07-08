using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Medbot.LoggingNS;

namespace Medbot {
    internal static class CommandsHandler {
        private static string commandsFilePath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Commands.xml";
        private static Points pointsObject;

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
                Console.WriteLine("FAILED to load commands. File not found");
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
                Console.WriteLine(ex);
                Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                return null;
            }

            return commandsList;
        }

        /// <summary>
        /// Decides which method should be executed for given command
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
                        Console.WriteLine(ex);
                        Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());

                        // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which add points} 
                        return String.Format(cmd.FailMessage, Points.CurrencyName, Points.CurrencyNamePlural, Points.CurrencyUnits, args[0], receiver != null ? receiver.DisplayName : args[1]);
                    }

                case HandlerType.Remove:
                    // Remove points, !removehoney 50 Bukk94 |  2 input args {0:Number of points} {1:User to which remove points}
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
                        Console.WriteLine(ex);
                        Logging.LogError(typeof(CommandsHandler), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());

                        // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which remove points} 
                        return String.Format(cmd.FailMessage, Points.CurrencyName, Points.CurrencyNamePlural, Points.CurrencyUnits, args[0], targetUser != null ? targetUser.DisplayName : args[1]);
                    }

                case HandlerType.Info:
                    // !med
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

        //private static List<string> ParseXMLlines(XElement input, string element) {
        //    List<string> parsedLines = input.Element(element).Value.Split('\n').ToList();
        //    parsedLines = parsedLines.Select(cmd => {
        //                                            cmd = cmd.Replace('\u0009'.ToString(), "").ToLower();  // \u0009 is \t - tabulator
        //                                            return cmd; 
        //                                          }).ToList();
        //    parsedLines.RemoveAll(x => x.Equals(""));
        //    return parsedLines;
        //}
    }
}
