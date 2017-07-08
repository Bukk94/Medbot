using System;
using System.IO;
using System.Reflection;

namespace Medbot.LoggingNS {
    public static class Logging {
        /// <summary>
        /// Path to Log folder where are all logs stored
        /// </summary>
        public static string LogFolderPath { get { return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Logs"; } }

        /// <summary>
        /// Full path to error log including the name
        /// </summary>
        public static string ErrorLog { get { return LogFolderPath + Path.DirectorySeparatorChar + "ErrorLog.txt"; } }

        /// <summary>
        /// Full path to event log including the name
        /// </summary>
        public static string EventLog { get { return LogFolderPath + Path.DirectorySeparatorChar + "EventLog.txt"; } }

        // Mutex locks
        private static Object errorLogLock = new Object();
        private static Object eventLogLock = new Object();

        /// <summary>
        /// Logs errors and warnings into file
        /// </summary>
        /// <param name="sender">Object which sent the log</param>
        /// <param name="methodSender">Method which sent the log request</param>
        /// <param name="errorMessage">Error message</param>
        public static void LogError(object sender, MethodBase methodSender, string errorMessage) {
            LogError(sender.GetType(), methodSender, errorMessage);
        }

        /// <summary>
        /// Logs errors and warnings into file
        /// </summary>
        /// <param name="sender">Type of object which sent the log</param>
        /// <param name="methodSender">Method which sent the log request</param>
        /// <param name="errorMessage">Error message</param>
        public static void LogError(Type sender, MethodBase methodSender, string errorMessage) {
            CheckLoggingFilesExistence();

            lock (errorLogLock) {
                try {
                    string fileContent = File.ReadAllText(ErrorLog);

                    fileContent += String.Format("{0}[{1} | {2}] Sender object: {3} # Sender method: {4} # Error log: {5}", Environment.NewLine, DateTime.Now.Date.ToShortDateString(),
                                                                             DateTime.Now.ToShortTimeString(),
                                                                             sender.FullName,
                                                                             methodSender.Name,
                                                                             errorMessage);
                    fileContent = fileContent.Replace("\n\n", "\n");
                    File.WriteAllText(ErrorLog, fileContent);
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Logs successfull event into file
        /// </summary>
        /// <param name="eventMessage">Event message to log</param>
        public static void LogEvent(MethodBase methodSender, string eventMessage) {
            CheckLoggingFilesExistence();

            lock (eventLogLock) {
                try {
                    string fileContent = File.ReadAllText(EventLog);

                    fileContent += String.Format("{0}[{1} | {2}] Sender method: {3} # {4}", Environment.NewLine, DateTime.Now.Date.ToShortDateString(),
                                                                                           DateTime.Now.ToShortTimeString(), methodSender.Name, eventMessage);

                    fileContent = fileContent.Replace("\n\n", "\n");
                    File.WriteAllText(EventLog, fileContent);
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                } 
            }
        }

        /// <summary>
        /// Checks loggin files existence, creates a new folder and files if missing
        /// </summary>
        private static void CheckLoggingFilesExistence() {
            if(!Directory.Exists(LogFolderPath))
                Directory.CreateDirectory(LogFolderPath);

            if (!File.Exists(ErrorLog))
                File.Create(ErrorLog);

            if (!File.Exists(EventLog))
                File.Create(EventLog);
        }
    }
}
