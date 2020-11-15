﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using Medbot.LoggingNS;
using Medbot.Internal;
using Medbot.Commands;
using System.Reflection;
using Medbot.Points;
using Medbot.Users;
using Medbot.Enums;

namespace Medbot
{
    internal enum DataType { Points, Experience }

    internal class FilesControl
    {
        private readonly BotDataManager _botDataManager;
        private readonly Object fileLock = new Object();
        private readonly Object settingsLock = new Object();

        public FilesControl(BotDataManager botDataManager)
        {
            _botDataManager = botDataManager;
        }

        internal List<Command> LoadCommands()
        {
            if (!File.Exists(_botDataManager.CommandsPath))
            {
                Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), "FAILED to load commands. File not found");
                return null;
            }

            List<Command> commandsList = new List<Command>();
            try
            {
                XDocument data = XDocument.Load(_botDataManager.CommandsPath);
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

                Logging.LogEvent(MethodBase.GetCurrentMethod(), "Commands Loaded");
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());
                return null;
            }

            return commandsList;
        }

        /// <summary>
        /// Loads data for specific user
        /// </summary>
        /// <param name="user">Reference to user which should be loaded</param>
        internal User LoadUserData(User user)
        {
            lock (fileLock)
            {
                // Loading
                Console.WriteLine("LOADING user profile " + user.DisplayName);
                if (!File.Exists(_botDataManager.DataPath))
                {
                    Logging.LogError(typeof(FilesControl), System.Reflection.MethodBase.GetCurrentMethod(), "Data loading of user" + user.DisplayName + " FAILED. FILE NOT FOUND.");
                    return user;
                }

                string username = user.Username;
                XDocument data = XDocument.Load(_botDataManager.DataPath);

                try
                {
                    XAttribute userRecord = data.Element("Medbot").Elements("User").Attributes("Username").FirstOrDefault(att => att.Value == username);

                    // TODO: Is it really necessary to throw exception here??
                    if (userRecord == null)
                        throw new Exception("Data loading of user " + user.DisplayName + " FAILED. User not found");

                    user.Points = long.Parse(userRecord.Parent.Attribute("Points").Value);
                    user.Experience = long.Parse(userRecord.Parent.Attribute("Experience").Value);
                    user.CheckRankUp();

                    DateTime date;
                    user.LastMessage = DateTime.TryParse(userRecord.Parent.Attribute("LastMessage").Value, out date) ? date : (DateTime?)null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Logging.LogError(typeof(FilesControl), MethodBase.GetCurrentMethod(), ex.ToString());
                }
            }

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
                Console.WriteLine("SAVING DATA");

                // File exists, load it, apply new values and save it
                if (File.Exists(_botDataManager.DataPath))
                {
                    XDocument data = XDocument.Load(_botDataManager.DataPath);

                    try
                    {
                        foreach (User user in onlineUsers)
                        {
                            XAttribute userRecord = data.Element("Medbot").Elements("User").Attributes("Username").FirstOrDefault(att => att.Value == user.Username);

                            if (userRecord != null)
                            { // User exists in current XML, edit his value
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
                        Console.WriteLine(ex);
                        Logging.LogError(typeof(FilesControl), MethodBase.GetCurrentMethod(), ex.ToString());
                    }

                    // Don't save witout root!
                    if (data.Root == null)
                    {
                        Logging.LogError(typeof(FilesControl), MethodBase.GetCurrentMethod(), "Points were NOT saved! Method tried to save the XML without root !");
                        return;
                    }

                    data.Save(_botDataManager.DataPath);
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

            // Don't save witout root!
            if (doc.Root == null)
            {
                Logging.LogError(typeof(FilesControl), System.Reflection.MethodBase.GetCurrentMethod(), "Points were NOT saved! Method tried to save the XML without root !");
                return;
            }

            Logging.LogEvent(System.Reflection.MethodBase.GetCurrentMethod(), "Points file was sucessfully created");
            doc.Save(_botDataManager.DataPath);
        }

        /// <summary>
        /// Add user record to XML document
        /// </summary>
        /// <param name="doc">Reference to opened XDocument where user shoud be added</param>
        /// <param name="user">User that should be added to the XML document</param>
        internal void AddUserRecord(ref XDocument doc, User user)
        {
            XElement element = new XElement("User");
            element.Add(new XAttribute("Username", user.Username));
            element.Add(new XAttribute("Points", user.Points));
            element.Add(new XAttribute("Experience", user.Experience));
            element.Add(new XAttribute("LastMessage", user.LastMessage.ToString())); // Empty string if null
            doc.Element("Medbot").Add(element);
        }

        internal async Task<bool> LoadLoginCredentials()
        {
            lock (settingsLock)
            {
                if (!File.Exists(_botDataManager.SettingsPath))
                {
                    PointsManager.LoadDefaultCurrencyDetails();
                    LoadDefaultDictionary();
                    return false;
                }

                try
                {
                    XDocument dataRaw = XDocument.Load(_botDataManager.SettingsPath);
                    var data = dataRaw.Element("Medbot").Element("Login");

                    // Load credentials
                    Login.BotName = data.Element("BotName") != null ? data.Element("BotName").Value : "";
                    Login.BotOauth = data.Element("Oauth") != null ? data.Element("Oauth").Value : "";
                    Login.BotIrcOAuth = data.Element("IrcOauth") != null ? data.Element("IrcOauth").Value : "";
                    Login.Channel = data.Element("Channel") != null ? data.Element("Channel").Value : "";

                    // Optional
                    Login.BotFullTwitchName = data.Element("BotFullName") != null ? data.Element("BotFullName").Value : "";
                    Login.ClientID = data.Element("ClientID") != null ? data.Element("ClientID").Value : "";
                }
                catch (Exception ex)
                {
                    Logging.LogError(typeof(FilesControl), MethodBase.GetCurrentMethod(), ex.ToString());
                    return false;
                }
            }

            // Generate ClientID if it's not set, causing a few sec freeze during waiting on server response
            if (String.IsNullOrEmpty(Login.ClientID))
                Login.ClientID = await Requests.GetClientID(Login.BotOauth);

            return Login.IsLoginCredentialsValid;
        }

        /// <summary>
        /// Loads bot dictionary from XML file
        /// </summary>
        /// <returns>Bool if successfully loaded all strings</returns>
        internal bool LoadBotDictionary()
        {
            lock (settingsLock)
            {
                if (!File.Exists(_botDataManager.SettingsPath))
                {
                    PointsManager.LoadDefaultCurrencyDetails();
                    LoadDefaultDictionary();
                    return false;
                }

                try
                {
                    XDocument dataRaw = XDocument.Load(_botDataManager.SettingsPath);
                    var data = dataRaw.Element("Medbot").Element("Dictionary");

                    // Load bot's dictionary
                    BotDictionary.WelcomeMessage = data.Element("WelcomeMessage") != null ? data.Element("WelcomeMessage").Value : "";
                    BotDictionary.GoodbyeMessage = data.Element("GoodbyeMessage") != null ? data.Element("GoodbyeMessage").Value : "";
                    BotDictionary.NewRankMessage = data.Element("NewRankMessage") != null ? data.Element("NewRankMessage").Value : "";
                    BotDictionary.CommandsNotFound = data.Element("CommandsNotFound") != null ? data.Element("CommandsNotFound").Value : "";
                    BotDictionary.ZeroCommands = data.Element("ZeroCommands") != null ? data.Element("ZeroCommands").Value : "";
                    BotDictionary.Yes = data.Element("Yes") != null ? data.Element("Yes").Value : "Yes";
                    BotDictionary.No = data.Element("No") != null ? data.Element("No").Value : "No";
                    BotDictionary.LeaderboardTopNumber = data.Element("LeaderboardTopNumber") != null ? int.Parse(data.Element("LeaderboardTopNumber").Value) : 3;
                    BotDictionary.GambleWinPercentage = data.Element("GambleWinPercentage") != null ? int.Parse(data.Element("GambleWinPercentage").Value) : 20;
                    BotDictionary.GambleBonusWinPercentage = data.Element("GambleBonusWinPercentage") != null ? int.Parse(data.Element("GambleBonusWinPercentage").Value) : 2;

                    // Percentage is incorrectly set, exceeding 100%. Load default
                    if (BotDictionary.GambleBonusWinPercentage + BotDictionary.GambleWinPercentage >= 100)
                    {
                        BotDictionary.GambleWinPercentage = 20;
                        BotDictionary.GambleBonusWinPercentage = 2;
                    }


                    // Load currency details
                    var currency = dataRaw.Element("Medbot").Element("Currency");
                    PointsManager.CurrencyName = currency.Attribute("Name") != null ? currency.Attribute("Name").Value : "gold";
                    PointsManager.CurrencyNamePlural = currency.Attribute("Plural") != null ? currency.Attribute("Plural").Value : "golds";
                    PointsManager.CurrencyUnits = currency.Attribute("Units") != null ? currency.Attribute("Units").Value : "g";

                    Logging.LogEvent(MethodBase.GetCurrentMethod(), "Bot dictionary and currency details were load successfully");
                }
                catch (Exception ex)
                {
                    Logging.LogError(typeof(FilesControl), MethodBase.GetCurrentMethod(), ex.ToString());
                    PointsManager.LoadDefaultCurrencyDetails();
                    LoadDefaultDictionary();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Loads users blacklist, usernames are in lowercase
        /// </summary>
        internal List<string> LoadUsersBlacklist()
        {
            if (!File.Exists(_botDataManager.SettingsPath))
                return new List<string>();

            lock (settingsLock)
            {
                try
                {
                    XDocument dataRaw = XDocument.Load(_botDataManager.SettingsPath);
                    var data = dataRaw.Element("Medbot").Element("Blacklist");

                    List<string> blacklist = data.Value.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    blacklist = blacklist.Select(val => val.Replace('\u0009'.ToString(), "").ToLower()).ToList();
                    blacklist = blacklist.Where(val => !String.IsNullOrEmpty(val)).Distinct().ToList();

                    return blacklist;
                }
                catch (Exception ex)
                {
                    Logging.LogError(typeof(FilesControl), MethodBase.GetCurrentMethod(), ex.ToString());
                    return new List<string>();
                }
            }
        }

        /// <summary>
        /// Loads bot intervals
        /// </summary>
        /// <returns>Returns Dictionary<string, int> containing intervals and values</returns>
        internal Dictionary<string, int> LoadBotIntervals()
        {
            Dictionary<string, int> intervals = new Dictionary<string, int>();
            lock (settingsLock)
            {
                if (!File.Exists(_botDataManager.SettingsPath))
                    return LoadDefaultIntervals();

                try
                {
                    XDocument dataRaw = XDocument.Load(_botDataManager.SettingsPath);
                    var pointsData = dataRaw.Element("Medbot").Element("Currency");
                    var expData = dataRaw.Element("Medbot").Element("Experience");

                    // Load currency data
                    intervals.Add("PointsInterval", pointsData.Attribute("Interval") != null ? int.Parse(pointsData.Attribute("Interval").Value) : 1);
                    intervals.Add("PointsIdleTime", pointsData.Attribute("IdleTime") != null ? int.Parse(pointsData.Attribute("IdleTime").Value) : 5);
                    intervals.Add("PointsPerTick", pointsData.Attribute("PointsPerTick") != null ? int.Parse(pointsData.Attribute("PointsPerTick").Value) : 1);
                    intervals.Add("PointsRewardIdles", pointsData.Attribute("RewardIdles") != null ? int.Parse(pointsData.Attribute("RewardIdles").Value) : 0);

                    // Load experience data
                    intervals.Add("ExperienceInterval", expData.Attribute("Interval") != null ? int.Parse(expData.Attribute("Interval").Value) : 1);
                    intervals.Add("ExperienceIdleExp", expData.Attribute("IdleExp") != null ? int.Parse(expData.Attribute("IdleExp").Value) : 1);
                    intervals.Add("ExperienceActiveExp", expData.Attribute("ActiveExp") != null ? int.Parse(expData.Attribute("ActiveExp").Value) : 5);
                    intervals.Add("ExperienceIdleTime", expData.Attribute("IdleTime") != null ? int.Parse(expData.Attribute("IdleTime").Value) : 5);

                    Logging.LogEvent(MethodBase.GetCurrentMethod(), "Intervals were loaded successfully");
                }
                catch (Exception ex)
                {
                    Logging.LogError(typeof(FilesControl), MethodBase.GetCurrentMethod(), ex.ToString());
                    intervals = LoadDefaultIntervals();
                }
            }

            return intervals;
        }

        /// <summary>
        /// Loads default bot intervals
        /// </summary>
        /// <returns>Returns dictionary of default intervals</returns>
        private Dictionary<string, int> LoadDefaultIntervals()
        {
            Dictionary<string, int> intervals = new Dictionary<string, int>();
            intervals.Add("PointsInterval", 1);
            intervals.Add("PointsIdleTime", 5);
            intervals.Add("PointsPerTick", 1);
            intervals.Add("PointsRewardIdles", 0);
            intervals.Add("ExperienceInterval", 1);
            intervals.Add("ExperienceIdleExp", 1);
            intervals.Add("ExperienceActiveExp", 5);
            intervals.Add("ExperienceIdleTime", 5);

            return intervals;
        }

        /// <summary>
        /// Loads default bot's dictionary
        /// </summary>
        private void LoadDefaultDictionary()
        {
            BotDictionary.Yes = "Yes";
            BotDictionary.No = "No";
            BotDictionary.LeaderboardTopNumber = 3;
        }

        /// <summary>
        /// Gets a points full leaderboard
        /// </summary>
        /// <returns>Sorted list of users</returns>
        internal List<TempUser> GetPointsLeaderboard()
        {
            List<TempUser> leaderboard = GetAllUsersSpecificInfo("Points");
            leaderboard.Sort(new LeaderboardComparer());
            leaderboard.Reverse();
            return leaderboard;
        }

        /// <summary>
        /// Gets experience full leaderboard
        /// </summary>
        /// <returns>Sorted list of users</returns>
        internal List<TempUser> GetExperienceLeaderboard()
        {
            List<TempUser> leaderboard = GetAllUsersSpecificInfo("Experience");
            // TODO: make singleton of the comparer
            leaderboard.Sort(new LeaderboardComparer());
            leaderboard.Reverse();
            return leaderboard;
        }

        /// <summary>
        /// Gets all user records from XML file where attribute is not null, empty and 0
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        internal List<TempUser> GetAllUsersSpecificInfo(string attribute)
        {
            lock (fileLock)
            {
                List<TempUser> usersList = new List<TempUser>();

                if (!File.Exists(_botDataManager.DataPath))
                    return usersList;

                try
                {
                    XDocument data = XDocument.Load(_botDataManager.DataPath);
                    var userRecords = data.Element("Medbot").Elements("User").Where(u =>
                    {
                        long numData = -1;
                        return u.Attribute(attribute) != null &&
                        (long.TryParse(u.Attribute(attribute).Value, out numData)) && numData > 0 &&
                        !u.Attribute("Username").Value.Equals(Login.BotName);
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
                    Logging.LogError(typeof(FilesControl), MethodBase.GetCurrentMethod(), ex.ToString());
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
                if (!File.Exists(_botDataManager.DataPath))
                    CreateNewDataFile(new List<User>());

                try
                {
                    XDocument data = XDocument.Load(_botDataManager.DataPath);
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

                    data.Save(_botDataManager.DataPath);
                }
                catch (Exception ex)
                {
                    Logging.LogError(typeof(FilesControl), System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                }
            }
        }
    }
}
