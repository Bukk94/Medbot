using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Medbot.Commands {
    enum CommandType { Points, XP, Internal }
    enum HandlerType { Info, Add, Remove, Leaderboard, LastFollower, Random }

    internal class Command {
        private CommandType commandType;
        private HandlerType handlerType;
        private string commandFormat;
        private string aboutCommand;
        private string successMessage;
        private string failMessage;
        private bool broadcasterOnly;
        private bool modRequired;
        private bool sendWhisper;

        /// <summary>
        /// Gets an command format e.g !points
        /// </summary>
        internal string CommandFormat { get { return this.commandFormat; } }

        /// <summary>
        /// Gets an command handler type (e.g. Add, Remove)
        /// </summary>
        internal HandlerType CommandHandlerType { get { return this.handlerType; } }

        /// <summary>
        /// Gets a boolean if the command can be executed only by broadcaster
        /// </summary>
        internal bool BroadcasterOnly { get { return this.broadcasterOnly; } }

        /// <summary>
        /// Gets a boolean if the moderator permissions are required to use the command
        /// </summary>
        internal bool ModeratorPermissionRequired { get { return this.modRequired; } }

        /// <summary>
        /// Gets command's fail message 
        /// </summary>
        internal string FailMessage { get { return this.failMessage; } }

        /// <summary>
        /// Gets command's success message
        /// </summary>
        internal string SuccessMessage { get { return this.successMessage; } }

        /// <summary>
        /// Gets bool if bot should send command's result as whisper
        /// </summary>
        internal bool SendWhisper { get { return this.sendWhisper; } }

        internal Command(CommandType cmd, HandlerType handler, string commandFormat, string about, string successMessage, 
                         string failMessage, bool broadcasterOnly, bool modPermission, bool sendWhisp) {
            this.handlerType = handler;
            this.commandFormat = commandFormat;
            this.aboutCommand = about;
            this.successMessage = successMessage;
            this.failMessage = failMessage;
            this.commandType = cmd;
            this.broadcasterOnly = broadcasterOnly;
            this.modRequired = modPermission;
            this.sendWhisper = sendWhisp;
        }

        /// <summary>
        /// Executes command
        /// </summary>
        /// <param name="sender">User who executed the command</param>
        /// <param name="command">Full name of the command</param>
        /// <returns>Informative string</returns>
        internal string Execute(User sender, List<string> args) {
            if (CheckCommandPermissions(sender))
                return CommandsHandler.ExecuteMethod(commandType, this, sender, args);
            else
                return String.Format("Nemáš dostatečná práva abys provedl tento příkaz. MedBot ti nepomůže.");
        }

        /// <summary>
        /// Checks if user has permission to use the command
        /// </summary>
        /// <param name="sender">User who executed the command</param>
        /// <returns>Bool value if he has permission to use the command</returns>
        public bool CheckCommandPermissions(User sender) {
            return (BroadcasterOnly && sender.Broadcaster) || (ModeratorPermissionRequired && (sender.Moderator || sender.Broadcaster) || (!BroadcasterOnly && !ModeratorPermissionRequired));
        }

        /// <summary>
        /// Verifies command format. {0} is always a number, {1} is always string
        /// </summary>
        /// <param name="chatCommand">String command to check</param>
        /// <returns>Bool value if command format is correct</returns>
        internal bool VerifyFormat(string chatCommand) {
            string cmdFormat = this.CommandFormat.Replace("{0}", @"\d+");
            cmdFormat = cmdFormat.Replace("{1}", @"\w+");
            Regex rgx = new Regex("^" + cmdFormat +"$", RegexOptions.IgnoreCase);

            return rgx.IsMatch(chatCommand);
        }

        /// <summary>
        /// Returns command's info message as help
        /// </summary>
        /// <returns>String message</returns>
        internal string GetAboutInfoMessage() {
            // 3 params {0:Currency name} {1:plural} {2: units}
            return String.Format(aboutCommand, Points.CurrencyName, Points.CurrencyNamePlural, Points.CurrencyUnits);
        }
    }
}
