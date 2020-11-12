using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Medbot.Commands.Enums;
using Medbot.Internal;
using Medbot.Users;

namespace Medbot.Commands
{
    public class Command
    {
        private readonly CommandThrottling _throttler;
        private CommandType _commandType;
        private TimeSpan _cooldown;

        /// <summary>
        /// Gets an command format e.g !points
        /// </summary>
        internal string CommandFormat { get; }

        /// <summary>
        /// Gets an command handler type (e.g. Add, Remove)
        /// </summary>
        internal CommandHandlerType CommandHandlerType { get; }

        /// <summary>
        /// Gets a boolean if the command can be executed only by broadcaster
        /// </summary>
        internal bool BroadcasterOnly { get; }

        /// <summary>
        /// Gets a boolean if the moderator permissions are required to use the command
        /// </summary>
        internal bool ModeratorPermissionRequired { get; }

        /// <summary>
        /// Gets command's info message as help
        /// </summary>
        internal string AboutMessage { get; }

        /// <summary>
        /// Gets command's fail message 
        /// </summary>
        internal string FailMessage { get; }

        /// <summary>
        /// Gets command's error message 
        /// </summary>
        internal string ErrorMessage { get; }

        /// <summary>
        /// Gets command's success message
        /// </summary>
        internal string SuccessMessage { get; }

        /// <summary>
        /// Gets bool if bot should send command's result as whisper
        /// </summary>
        internal bool SendWhisper { get; }

        internal Command(CommandType cmd, CommandHandlerType handler, string commandFormat, string about, string successMessage,
                         string failMessage, string errorMessage, bool broadcasterOnly, bool modPermission, bool sendWhisp, TimeSpan cd)
        {
            this.CommandHandlerType = handler;
            this.CommandFormat = commandFormat;
            this.AboutMessage = about;
            this.SuccessMessage = successMessage;
            this.FailMessage = failMessage;
            this.ErrorMessage = errorMessage;
            this._commandType = cmd;
            this.BroadcasterOnly = broadcasterOnly;
            this.ModeratorPermissionRequired = modPermission;
            this.SendWhisper = sendWhisp;
            this._cooldown = cd;
            this._throttler = new CommandThrottling(this._cooldown, this);
        }

        /// <summary>
        /// Executes command
        /// </summary>
        /// <param name="sender">User who executed the command</param>
        /// <param name="command">Full name of the command</param>
        /// <returns>Informative string</returns>
        internal string Execute(User sender, List<string> args)
        {
            if (CheckCommandPermissions(sender) && _throttler.AllowedToExecute())
                return CommandsHandler.ExecuteMethod(_commandType, this, sender, args);
            //else
            //    return String.Format("Nemáš dostatečná práva abys provedl tento příkaz. MedBot ti nepomůže.");
            return "";
        }

        /// <summary>
        /// Checks if user has permission to use the command
        /// </summary>
        /// <param name="sender">User who executed the command</param>
        /// <returns>Bool value if he has permission to use the command</returns>
        public bool CheckCommandPermissions(User sender)
        {
            return (BroadcasterOnly && sender.IsBroadcaster) || (ModeratorPermissionRequired && (sender.IsModerator || sender.IsBroadcaster) || (!BroadcasterOnly && !ModeratorPermissionRequired));
        }

        /// <summary>
        /// Verifies command format. {0} is always a number, {1} is always string
        /// </summary>
        /// <param name="chatCommand">String command to check</param>
        /// <returns>Bool value if command format is correct</returns>
        internal bool VerifyFormat(string chatCommand)
        {
            string cmdFormat = this.CommandFormat.Replace("{0}", @"\d+");
            cmdFormat = cmdFormat.Replace("{1}", @"\w+");
            Regex regex = new Regex("^" + cmdFormat + "$", RegexOptions.IgnoreCase);

            return regex.IsMatch(chatCommand);
        }

        /// <summary>
        /// Resets command's cooldown
        /// </summary>
        internal void ResetCommandCooldown()
        {
            _throttler.ResetThrottlingTimer();
        }

        /// <summary>
        /// Gets readable format of command's format
        /// </summary>
        /// <returns>String of readable command</returns>
        internal string ToReadableFormat()
        {
            string readable = this.CommandFormat.Replace("{0}", "<0>");
            readable = readable.Replace("{1}", "<text>");
            return readable;
        }
    }
}
