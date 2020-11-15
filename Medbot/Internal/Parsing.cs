using Medbot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Medbot.Internal
{
    public static class Parsing
    {
        /// <summary>
        /// Gets a username from full chat informative string
        /// </summary>
        /// <param name="chatLine">Full chat line</param>
        /// <returns>Username of user who sent the message in lovercase</returns>
        public static string ParseUsername(string chatLine)
        {
            string sub = chatLine.Substring(chatLine.IndexOf('!') + 1);
            return sub.Substring(0, sub.IndexOf('@')) != String.Empty ? sub.Substring(0, sub.IndexOf('@')) : ParseDisplayName(chatLine).ToLower();
        }

        /// <summary>
        /// Parses Display Name from PRIVMSG chat line
        /// </summary>
        /// <param name="chatLine">Full chat line</param>
        /// <returns>User's display name</returns>
        public static string ParseDisplayName(string chatLine)
        {
            if (chatLine.IndexOf("display-name=") >= 0)
            {
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
        public static string ParseChatMessage(string chatLine)
        {
            string msg = chatLine.Substring(chatLine.IndexOf("PRIVMSG"));
            int start = msg.IndexOf(':');
            return msg.Substring(start + 1, msg.Length - start - 1).Trim();
        }

        /// <summary>
        /// Parses user's badges such as broadcaster, moderator, turbo, subscriber.
        /// Each badge has a name and version identifier (e.g. broadcaster/1 or moderator/1)
        /// </summary>
        /// <param name="chatLine">Full chat line with possible badges inside (badges=broadcaster/1,global_mod/1,turbo/6;)</param>
        /// <returns>Returns List of strings containing all user's badges </returns>
        public static List<Badges> ParseBadges(string chatLine)
        {
            var regexPattern = @"badges=(?<list>.+)\/\d;";
            var match = Regex.Match(chatLine, regexPattern);

            if (!match.Success)
                return null;

            // broadcaster/1,global_mod/1,turbo
            var badgesRawList = match.Groups["list"].Value;

            var badges = Regex.Split(badgesRawList, @"\/\d,?");

            return badges.Select(x => {
                if (Enum.TryParse<Badges>(x, true, out var badge)) return badge;
                Console.WriteLine($"WARNING: Unknown badge found: {x}!");
                return Badges.Unknown;
            }).ToList();
        }

        /// <summary>
        /// Parses command's input values and returns them as list
        /// </summary>
        /// <param name="command">String command from chat, not command object!</param>
        /// <returns>Returns only command arguments as List</returns>
        public static List<string> ParseCommandValues(string command)
        {
            return command.Split(' ').Skip(1).ToList();
        }

        /// <summary>
        /// Parses boolean value from given attribute
        /// </summary>
        /// <param name="element">XElement containing attribute to parse</param>
        /// <param name="attribute">Name of the attribute to parse from</param>
        /// <returns>Boolean value of the attribute, false if not found</returns>
        public static bool ParseBooleanFromAttribute(XElement element, string attribute)
        {
            if (!Boolean.TryParse(element.Attribute(attribute).Value, out bool parseBool))
                parseBool = false;

            return parseBool;
        }

        /// <summary>
        /// Parses TimeSpan value from given attribute
        /// </summary>
        /// <param name="element">XElement containing attribute to parse</param>
        /// <param name="attribute">Name of the attribute to parse from</param>
        /// <returns>TimeSpan object of the attribute</returns>
        public static TimeSpan ParseTimeSpanFromAttribute(XElement element, string attribute)
        {
            if (!TimeSpan.TryParse(element.Attribute(attribute).Value, out TimeSpan parseTimespan))
                parseTimespan = new TimeSpan();

            return parseTimespan;
        }
    }
}
