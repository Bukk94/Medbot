using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using Medbot.Internal;
using Medbot.Commands;
using Medbot.Points;
using Medbot.Users;
using Medbot.Enums;
using Medbot.ExpSystem;
using Medbot.Internal.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Medbot
{
    internal class FilesControl
    {
        private readonly ILogger _logger;
        private readonly object fileLock = new object();
        private readonly object settingsLock = new object();
        private readonly object dictionaryLock = new object();
        private readonly LeaderboardComparer LeaderboardComparer;

        public string SettingsPath => Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");
        public string DictionaryPath => Path.Combine(Directory.GetCurrentDirectory(), "Dictionary.json");
        public string DataPath => Path.Combine(Directory.GetCurrentDirectory(), "Users_data.xml");
        public string CommandsPath => Path.Combine(Directory.GetCurrentDirectory(), "Commands.xml");
        public string RanksPath => Path.Combine(Directory.GetCurrentDirectory(), "Ranks.txt");

        public FilesControl()
        {
            _logger = Logging.GetLogger<FilesControl>();
            LeaderboardComparer = new LeaderboardComparer();
        }

        internal List<Command> LoadCommands()
        {
            if (!File.Exists(CommandsPath))
            {
                _logger.LogError("Failed to load commands. Commands file not found at '{path}'.", CommandsPath);
                return null;
            }
            
            var commandsList = new List<Command>();
            try
            {
                XDocument data = XDocument.Load(CommandsPath);
                var commandsTypeGroups = data.Element("Medbot").Elements("Commands");

                foreach (var cmdGroup in commandsTypeGroups)
                {
                    CommandType cmdType = (CommandType)Enum.Parse(typeof(CommandType), cmdGroup.Attribute("Type").Value);
                    var commands = cmdGroup.Elements("Command");

                    foreach (var cmd in commands)
                    {
                        if (!Enum.TryParse<CommandHandlerType>(cmd.Attribute("Handler").Value, out CommandHandlerType handler))
                            continue;

                        bool broadcasterOnly = Parsing.ParseBooleanFromAttribute(cmd, "BroadcasterOnly");

                        Command newCmd = new Command(cmdType, handler, cmd.Value.ToLower(),
                                                     cmd.Attribute("AboutCommand").Value,
                                                     cmd.Attribute("SuccessMessage").Value,
                                                     cmd.Attribute("FailMessage").Value,
                                                     cmd.Attribute("ErrorMessage") != null ? cmd.Attribute("ErrorMessage").Value : "",
                                                     broadcasterOnly,
                                                     broadcasterOnly ? false : Parsing.ParseBooleanFromAttribute(cmd, "ModPermissionRequired"),
                                                     Parsing.ParseBooleanFromAttribute(cmd, "SendWhisper"),
                                                     Parsing.ParseTimeSpanFromAttribute(cmd, "Cooldown")
                                                     );
                        commandsList.Add(newCmd);
                    }
                }

                _logger.LogInformation("Commands loaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Fatal error occurred during loading commands.\n{ex}", ex);
                return null;
            }

            return commandsList;
        }

        /// <summary>
        /// Loads ranks from text file
        /// </summary>
        internal List<Rank> LoadRanks()
        {
            var ranks = new List<Rank>();

            if (!File.Exists(RanksPath))
            {
                _logger.LogError("Loading ranks failed. File not found at '{path}'!", RanksPath);
                return ranks;
            }

            string[] dataRaw = File.ReadAllText(RanksPath).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int level = 1;

            foreach (string data in dataRaw)
            {
                // Format Exp (space) rankname:  500 RankName
                try
                {
                    var rankData = data.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    ranks.Add(new Rank(rankData[1], level++, long.Parse(rankData[0])));
                }
                catch (Exception ex)
                {
                    _logger.LogError("Fatal error occurred during loading ranks.\n{ex}", ex);
                    continue;
                }
            }

            return ranks;
        }

        /// <summary>
        /// Loads data for specific user
        /// </summary>
        /// <param name="user">Reference to user which should be loaded</param>
        internal async Task<User> LoadUserData(User user)
        {
            lock (fileLock)
            {
                _logger.LogInformation("Loading user profile '{user}'.", user.DisplayName);
                if (!File.Exists(DataPath))
                {
                    _logger.LogError("Loading data for user {user} failed. File not found at '{path}'!", user.DisplayName, DataPath);
                    return user;
                }

                string username = user.Username;
                XDocument data = XDocument.Load(DataPath);

                try
                {
                    XAttribute userRecord = data.Element("Medbot").Elements("User").Attributes("Username").FirstOrDefault(att => att.Value == username);

                    // TODO: Is it really necessary to throw exception here??
                    if (userRecord == null)
                        throw new Exception("Data loading of user " + user.DisplayName + " FAILED. User not found");

                    var fileUserId = userRecord.Parent.Attribute("ID")?.Value;
                    user.ID = fileUserId != null ? long.Parse(fileUserId) : user.ID;
                    user.Points = long.Parse(userRecord.Parent.Attribute("Points").Value);
                    user.Experience = long.Parse(userRecord.Parent.Attribute("Experience").Value);
                    user.LastMessage = DateTime.TryParse(userRecord.Parent.Attribute("LastMessage").Value, out DateTime date) ? date : (DateTime?)null;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Fatal error occurred during loading user data.\n{ex}", ex);
                }
            }

            if (user.ID <= 0)
                await user.UpdateUserId();

            return user;
        }

        /// <summary>
        /// Saves all data to XML file
        /// </summary>
        internal void SaveData(List<User> onlineUsers)
        {
            lock (fileLock)
            {
                // Saving
                _logger.LogInformation("SAVING DATA...");

                // File exists, load it, apply new values and save it
                if (File.Exists(DataPath))
                {
                    XDocument data = XDocument.Load(DataPath);

                    try
                    {
                        foreach (User user in onlineUsers)
                        {
                            XAttribute userRecord = data.Element("Medbot").Elements("User").Attributes("Username").FirstOrDefault(att => att.Value == user.Username);

                            if (userRecord != null)
                            { // User exists in current XML, edit his value
                                if (userRecord.Parent.Attribute("ID") == null)
                                    userRecord.Parent.Add(new XAttribute("ID", user.ID));
                                else
                                    userRecord.Parent.Attribute("ID").Value = user.ID.ToString();
                                
                                userRecord.Parent.Attribute("Points").Value = user.Points.ToString();
                                userRecord.Parent.Attribute("Experience").Value = user.Experience.ToString();
                                userRecord.Parent.Attribute("LastMessage").Value = user.LastMessage.ToString();
                            }
                            else
                            { // User doesn't exist, create a new record
                                AddUserRecord(ref data, user);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Fatal error occurred during saving data.\n{ex}", ex);
                    }

                    // Don't save without root!
                    if (data.Root == null)
                    {
                        _logger.LogError("Points were not saved! Method tried to save the XML without root!");
                        return;
                    }

                    data.Save(DataPath);
                }
                else
                { // File doesn't exist, create a new one, save all values
                    CreateNewDataFile(onlineUsers);
                }
            }
        }

        /// <summary>
        /// Creates a new file if the file does not exist
        /// </summary>
        internal void CreateNewDataFile(List<User> onlineUsers)
        {
            XDocument doc = new XDocument(new XElement("Medbot"));

            foreach (User user in onlineUsers)
            {
                AddUserRecord(ref doc, user);
            }

            // Don't save without root!
            if (doc.Root == null)
            {
                _logger.LogError("Points were not saved! Method tried to save the XML without root!");
                return;
            }

            doc.Save(DataPath);
            _logger.LogInformation("Points file was sucessfully created at '{path}'.", DataPath);
        }

        /// <summary>
        /// Add user record to XML document
        /// </summary>
        /// <param name="doc">Reference to opened XDocument where user shoud be added</param>
        /// <param name="user">User that should be added to the XML document</param>
        internal void AddUserRecord(ref XDocument doc, User user)
        {
            XElement element = new XElement("User");
            element.Add(new XAttribute("ID", user.ID));
            element.Add(new XAttribute("Username", user.Username));
            element.Add(new XAttribute("Points", user.Points));
            element.Add(new XAttribute("Experience", user.Experience));
            element.Add(new XAttribute("LastMessage", user.LastMessage.ToString())); // Empty string if null
            doc.Element("Medbot").Add(element);
        }

        internal AllSettings LoadAllSettings()
        {
            if (!File.Exists(SettingsPath))
            {
                return new AllSettings
                {
                    Settings = new BotSettings(),
                    Login = new LoginDetails(),
                    Blacklist = new List<string>(),
                    Currency = new CurrencySettings(),
                    Experience = new ExperienceSettings()
                };
            }

            var json = File.ReadAllText(SettingsPath);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<AllSettings>(json);
        }

        /// <summary>
        /// Loads bot dictionary from JSON file
        /// </summary>
        /// <returns>Returns dictionary object</returns>
        internal DictionaryStrings LoadBotDictionary()
        {
            if (!File.Exists(DictionaryPath))
                return LoadDefaultDictionary();

            lock (dictionaryLock)
            {
                var dictionary = Parsing.Deserialize<DictionaryStrings>(File.ReadAllText(DictionaryPath));
                return dictionary ?? LoadDefaultDictionary();
            }
        }

        /// <summary>
        /// Loads default bot's dictionary
        /// </summary>
        private DictionaryStrings LoadDefaultDictionary()
        {
            return new DictionaryStrings
            {
                Yes = "Yes",
                No = "No",
                BotRespondMessages = new string[0],
                ZeroCommands = "I'm helpless! I have no active commands! Notify the broadcaster!",
                InsufficientPermissions = "You do not have enough permissions to do that!",
                NewRankMessage = "{0} got a new rank: [{1}] {2}!",
                CommandsNotFound = "Something went wrong! I can't find the file with all the commands!"
            };
        }

        /// <summary>
        /// Gets a points full leaderboard
        /// </summary>
        /// <returns>Sorted list of users</returns>
        internal List<TempUser> GetPointsLeaderboard(string botname)
        {
            List<TempUser> leaderboard = GetAllUsersSpecificInfo(DataType.Points, botname);
            leaderboard.Sort(LeaderboardComparer);
            leaderboard.Reverse();

            return leaderboard;
        }

        /// <summary>
        /// Gets experience full leaderboard
        /// </summary>
        /// <returns>Sorted list of users</returns>
        internal List<TempUser> GetExperienceLeaderboard(string botname)
        {
            List<TempUser> leaderboard = GetAllUsersSpecificInfo(DataType.Experience, botname);
            leaderboard.Sort(LeaderboardComparer);
            leaderboard.Reverse();

            return leaderboard;
        }

        /// <summary>
        /// Gets all user records from XML file where attribute is not null, empty or 0
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        internal List<TempUser> GetAllUsersSpecificInfo(DataType dataType, string botname)
        {
            lock (fileLock)
            {
                var usersList = new List<TempUser>();

                if (!File.Exists(DataPath))
                    return usersList;

                var attribute = dataType.ToString();
                try
                {
                    XDocument data = XDocument.Load(DataPath);
                    var userRecords = data.Element("Medbot").Elements("User").Where(u =>
                    {
                        long numData = -1;
                        return u.Attribute(attribute) != null &&
                        (long.TryParse(u.Attribute(attribute).Value, out numData)) && numData > 0 &&
                        !u.Attribute("Username").Value.Equals(botname);
                    }) //Exclude bot from leaderboard
                    .ToList();

                    foreach (XElement user in userRecords)
                    {
                        usersList.Add(new TempUser(user.Attribute("Username").Value, user.Attribute(attribute).Value));
                    }

                    return usersList;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Fatal error occurred during loading user specific info.\n{ex}", ex);
                    return new List<TempUser>();
                }
            }
        }

        /// <summary>
        /// Adds number of points to user stored in the file, if user does not exist, creates a new one
        /// </summary>
        /// <param name="username">Username to search and change</param>
        /// <param name="expToAdd">Number of points to add</param>
        internal void AddUserPointsToFile(string username, long pointsToAdd)
        {
            FileDataManipulation(username, pointsToAdd, DataType.Points, (x, y) => x + y);
        }

        /// <summary>
        /// Removes number of points to user stored in the file, if user does not exist, creates a new one
        /// </summary>
        /// <param name="username">Username to search and change</param>
        /// <param name="pointsToRemove">Number of points to remove</param>
        internal void RemoveUserPointsFromFile(string username, long pointsToRemove)
        {
            FileDataManipulation(username, pointsToRemove, DataType.Points, (x, y) => { return x - y >= 0 ? x - y : 0; });
        }

        /// <summary>
        /// Adds number of experience to user stored in the file, if user does not exist, creates a new one
        /// </summary>
        /// <param name="username">Username to search and change</param>
        /// <param name="expToAdd">Number of points to add</param>
        internal void AddUserExperienceToFile(string username, long expToAdd)
        {
            FileDataManipulation(username, expToAdd, DataType.Experience, (x, y) => x + y);
        }

        /// <summary>
        /// Method which manipulates with points in file
        /// </summary>
        /// <param name="username">Username to search and change</param>
        /// <param name="pointsToChange">Number of points to change</param>
        /// <param name="operation">Function with 2 long params to define points operation</param>
        private void FileDataManipulation(string username, long dataToChange, DataType type, Func<long, long, long> operation)
        {
            lock (fileLock)
            {
                if (!File.Exists(DataPath))
                    CreateNewDataFile(new List<User>());

                try
                {
                    XDocument data = XDocument.Load(DataPath);
                    XElement userRecord = data.Element("Medbot").Elements("User").Attributes("Username").FirstOrDefault(att => att.Value == username.ToLower()).Parent;

                    if (userRecord != null)
                    { // User exists in current XML, edit his value
                        userRecord.Attribute(type.ToString()).Value = operation(long.Parse(userRecord.Attribute(type.ToString()).Value), dataToChange).ToString();
                    }
                    else
                    { // User doesn't exist, create a new record
                        User newUser = null;

                        if (type == DataType.Points)
                            newUser = new User(username.ToLower(), operation(0, dataToChange), 0);
                        else
                            newUser = new User(username.ToLower(), 0, operation(0, dataToChange));

                        AddUserRecord(ref data, newUser);
                    }

                    // Don't save witout root!
                    if (data.Root == null)
                        throw new Exception("Points were NOT saved! Method tried to save the XML without root !");

                    data.Save(DataPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Fatal error occurred during user data manipulation!\n{ex}", ex);
                }
            }
        }
    }
}
