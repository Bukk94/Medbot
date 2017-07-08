using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Medbot.LoggingNS;

namespace Medbot {
    public class Points {
        private bool rewardIdles;
        private int interval; // ms
        private int amount;   // number
        private int idleTime; // s
        private Timer timer;
        private Object fileLock;
        private string pointsLocationPath;
        private string pointsFileName = "Activity Points.xml";
        private static string currencyName = String.Empty;
        private static string currencyNamePlural = String.Empty;
        private static string currencyUnits = String.Empty;

        /// <summary>
        /// Amount added to users each tick
        /// </summary>
        public int Amount { get { return this.amount; } set { this.amount = value; } }

        /// <summary>
        /// Sets/Gets Idle time before stopping rewarding the user
        /// </summary>
        public int IdleTime { get { return this.idleTime; } set { this.idleTime = value; } }

        /// <summary>
        /// Gets/Sets currency name
        /// </summary>
        public static string CurrencyName { get { return currencyName; } set { currencyName = value; } }

        /// <summary>
        /// Gets/Sets currency plural name
        /// </summary>
        public static string CurrencyNamePlural { get { return currencyNamePlural; } set { currencyNamePlural = value; } }

        /// <summary>
        /// Gets/Sets currency units
        /// </summary>
        public static string CurrencyUnits { get { return currencyUnits; } set { currencyUnits = value; } }

        /// <summary>
        /// Point class manages point awarding and timer ticking
        /// </summary>
        /// <param name="onlineUsers">Reference to List of online users</param>
        /// <param name="interval">The time interval between each tick in ms</param>
        /// <param name="rewardIdles">Bool if idle users should be rewarded</param>
        /// <param name="idleTime">Time after which the user will become idle (in seconds)</param>
        /// <param name="amount">Amount of points awarded to active users each tick</param>
        internal Points(int interval, bool rewardIdles = false, int idleTime = 5, int amount = 1) {
            this.interval = interval;
            this.amount = amount;
            this.idleTime = idleTime;
            this.rewardIdles = rewardIdles;
            this.pointsLocationPath = String.Format(@"{0}{1}{2}", Directory.GetCurrentDirectory(), Path.DirectorySeparatorChar, this.pointsFileName);
            this.timer = new Timer(AwardPoints_TimerTick, null, 0, this.interval); // Timeout.Infinite ?
            this.fileLock = new Object();

            LoadCurrencyDetails();
        }

        /// <summary>
        /// Award points to active users
        /// </summary>
        private void AwardPoints_TimerTick(Object state) {
            if (BotClient.OnlineUsers == null || BotClient.OnlineUsers.Count <= 0)
                return;

            Console.WriteLine("Timer Points ticked, Number of users: " + BotClient.OnlineUsers.Count);
            foreach (User u in BotClient.OnlineUsers) {
                if (u.LastMessage != null && (DateTime.Now - u.LastMessage < TimeSpan.FromSeconds(this.idleTime) || rewardIdles)) {
                    u.Points += this.amount;
                    Console.WriteLine("Rewarding " + u.Username + " for activity");
                }
            }
            
            SavePoints();
        }

        /// <summary>
        /// Loads currency details from Settings file
        /// </summary>
        internal void LoadCurrencyDetails() {
            if (!File.Exists(BotClient.SettingsPath)) {
                Console.WriteLine("Currency failed to load");
                Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), "Currency failed to load. Missing Settings.xml");
                return;
            }

            lock (BotClient.settingsLock) {
                try {
                    XDocument data = XDocument.Load(BotClient.SettingsPath);
                    var currency = data.Element("Medbot").Element("Currency");
                    CurrencyName = currency.Attribute("name").Value;
                    CurrencyNamePlural = currency.Attribute("plural").Value;
                    CurrencyUnits = currency.Attribute("units").Value;
                    Logging.LogEvent(System.Reflection.MethodBase.GetCurrentMethod(), "Currency details were load successfully");
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                }
            }
        }

        /// <summary>
        /// Loads points for specific user
        /// </summary>
        /// <param name="user">Reference to user which should be loaded</param>
        internal void LoadUserPoints(ref User user) {
            lock (fileLock) {
                // Loading
                Console.WriteLine("LOADING user points");
                if (!File.Exists(pointsLocationPath)) {
                    Console.WriteLine("Loading of user's" + user.DisplayName + " points FAILED");
                    Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), "Loading of user's" + user.DisplayName + " points FAILED");
                    return;
                }

                string username = user.Username;
                XDocument data = XDocument.Load(pointsLocationPath);

                try {
                    XAttribute userRecord = data.Element("Medbot").Elements("User").Attributes("Username").FirstOrDefault(att => att.Value == username);
                    if (userRecord != null) {
                        user.Points = long.Parse(userRecord.NextAttribute.Value);

                        DateTime date;
                        user.LastMessage = DateTime.TryParse(userRecord.NextAttribute.NextAttribute.Value, out date) ? date : (DateTime?)null;
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                }
            }
        }

        /// <summary>
        /// Saves all points to XML file
        /// </summary>
        public void SavePoints() {
            lock (fileLock) {
                // Saving
                Console.WriteLine("SAVING POINTS");

                // File exists, load it, apply new values and save it
                if (File.Exists(pointsLocationPath)) {
                    XDocument data = XDocument.Load(pointsLocationPath);

                    try {
                        foreach (User user in BotClient.OnlineUsers) {
                            XAttribute userRecord = data.Element("Medbot").Elements("User").Attributes("Username").FirstOrDefault(att => att.Value == user.Username);
                            if (userRecord != null) { // User exists in current XML, edit his value
                                userRecord.NextAttribute.Value = user.Points.ToString(); // Points attribute
                                userRecord.NextAttribute.NextAttribute.Value = user.LastMessage.ToString(); // LastMessage attribute
                            } else { // User doesn't exist, create a new record
                                AddUserRecord(ref data, user);
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine(ex);
                        Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                    }

                    // Don't save witout root!
                    if (data.Root == null) {
                        Console.WriteLine("Points were NOT saved! XML Root is missing!");
                        Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), "Method tried to save the XML without root !");
                        return;
                    }

                    Logging.LogEvent(System.Reflection.MethodBase.GetCurrentMethod(), "Points were saved successfully");
                    data.Save(pointsLocationPath);
                } else { // File doesn't exist, create a new one, save all values
                    CreateNewPointsFile();
                }
            }
        }

        /// <summary>
        /// Creates a new file if the file does not exist
        /// </summary>
        private void CreateNewPointsFile() {
            XDocument doc = new XDocument(new XElement("Medbot"));

            foreach (User user in BotClient.OnlineUsers) {
                AddUserRecord(ref doc, user);
            }

            // Don't save witout root!
            if (doc.Root == null) {
                Console.WriteLine("Points were NOT saved! XML Root is missing!");
                Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), "Method tried to save the XML without root !");
                return;
            }

            Logging.LogEvent(System.Reflection.MethodBase.GetCurrentMethod(), "Points file was sucessfully created");
            doc.Save(pointsLocationPath);
        }

        /// <summary>
        /// Add user record to XML document
        /// </summary>
        /// <param name="doc">Reference to opened XDocument where user shoud be added</param>
        /// <param name="user">User that should be added to the XML document</param>
        public void AddUserRecord(ref XDocument doc, User user) {
            XElement element = new XElement("User");
            element.Add(new XAttribute("Username", user.Username));
            element.Add(new XAttribute("Points", user.Points));
            element.Add(new XAttribute("LastMessage", user.LastMessage.ToString())); // Empty string if null
            doc.Element("Medbot").Add(element);
        }

        /// <summary>
        /// Adds number of points to user stored in the file, if user does not exist, creates a new one
        /// </summary>
        /// <param name="username">Username to search and change</param>
        /// <param name="pointsToAdd">Number of points to add</param>
        public void AddUserPointsToFile(string username, long pointsToAdd) {
            FilePointsManipulation(username, pointsToAdd, (x, y) => x + y);
        }

        /// <summary>
        /// Removes number of points to user stored in the file, if user does not exist, creates a new one
        /// </summary>
        /// <param name="username">Username to search and change</param>
        /// <param name="pointsToRemove">Number of points to remove</param>
        public void RemoveUserPointsFromFile(string username, long pointsToRemove) {
            FilePointsManipulation(username, pointsToRemove, (x, y) => { return x - y >= 0 ? x - y : 0; });
        }

        /// <summary>
        /// Method which manipulates with points in file
        /// </summary>
        /// <param name="username">Username to search and change</param>
        /// <param name="pointsToChange">Number of points to change</param>
        /// <param name="operation">Function with 2 long params to define points operation</param>
        private void FilePointsManipulation(string username, long pointsToChange, Func<long, long, long> operation) {
            lock (fileLock) {
                if (!File.Exists(pointsLocationPath))
                    CreateNewPointsFile();

                try {
                    XDocument data = XDocument.Load(pointsLocationPath);
                    XAttribute userRecord = data.Element("Medbot").Elements("User").Attributes("Username").FirstOrDefault(att => att.Value == username.ToLower());
                    if (userRecord != null) { // User exists in current XML, edit his value
                        userRecord.NextAttribute.Value = operation(long.Parse(userRecord.NextAttribute.Value), pointsToChange).ToString();
                    } else { // User doesn't exist, create a new record
                        User newUser = new User(username.ToLower(), pointsToChange);
                        AddUserRecord(ref data, newUser);
                    }

                    // Don't save witout root!
                    if (data.Root == null) {
                        Console.WriteLine("Points were NOT saved! XML Root is missing!");
                        Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), "Method tried to save the XML without root !");
                        return;
                    }

                    data.Save(pointsLocationPath);
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    Logging.LogError(this, System.Reflection.MethodBase.GetCurrentMethod(), ex.ToString());
                }
            }
        }
    }
}
