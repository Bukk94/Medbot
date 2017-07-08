using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Medbot {
    public static class Parsing {

        /// <summary>
        /// Gets a username from full chat informative string
        /// </summary>
        /// <param name="chatLine">Full chat line</param>
        /// <returns>Username of user who sent the message in lovercase</returns>
        public static string ParseUsername(string chatLine) {
            string sub = chatLine.Substring(chatLine.IndexOf('!') + 1);
            return sub.Substring(0, sub.IndexOf('@'));
        }

        /// <summary>
        /// Parses Display Name from PRIVMSG chat line
        /// </summary>
        /// <param name="chatLine">Full chat line</param>
        /// <returns>User's display name</returns>
        public static string ParseDisplayName(string chatLine) {
            if (chatLine.IndexOf("display-name=") >= 0) {
                string sub = chatLine.Substring(chatLine.IndexOf("display-name="));
                sub = sub.Replace("display-name=", "");
                return sub.Substring(0, sub.IndexOf(';'));
            }

            return String.Empty;
        }

        /// <summary>
        /// Gets only chat message from full chat informative string
        /// </summary>
        /// <param name="chatLine">Full chat line including chat message</param>
        /// <returns>Plain message</returns>
        public static string ParseChatMessage(string chatLine) {
            string msg = chatLine.Substring(chatLine.IndexOf("PRIVMSG"));
            int start = msg.IndexOf(':');
            return msg.Substring(start + 1, msg.Length - start - 1).Trim();
        }

        /// <summary>
        /// Parses user's badges such as broadcaster, moderator, turbo, subscriber.
        /// </summary>
        /// <param name="chatLine">Full chat line</param>
        /// <returns>Returns List of strings containing all user's badges </returns>
        public static List<string> ParseBadges(string chatLine) {
            if (!chatLine.Contains("@badges"))
                return null;

            // TEST: Test correct parsing of multiple badges
            string parse = chatLine.Substring(chatLine.IndexOf("@badges=") + 8, chatLine.IndexOf(';') - 8);
            parse = parse.Replace("1", "");
            List<string> badges = parse.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            return badges;
        }

        /// <summary>
        /// Parses command's input values and returns them as list
        /// </summary>
        /// <param name="command">String command from chat, not command object!</param>
        /// <returns>Returns only command arguments as List</returns>
        public static List<string> ParseCommandValues(string command) {
            return command.Split(' ').Skip(1).ToList();
        }

        /// <summary>
        /// Parses boolean value from given attribute
        /// </summary>
        /// <param name="el">XElement containing attribute to parse</param>
        /// <param name="attribute">Name of the attribute to parse from</param>
        /// <returns>Boolean value of the attribute, false if not found</returns>
        public static bool ParseBooleanFromAttribute(XElement el, string attribute) {
            bool parseBool;
            if (!Boolean.TryParse(el.Attribute(attribute).Value, out parseBool))
                parseBool = false;

            return parseBool;
        }
    }
}
