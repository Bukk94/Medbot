﻿using System;

namespace Medbot.Internal.Models
{
    internal class LoginDetails
    {
        /// <summary>
        /// Token to bot's account
        /// Tokan contains oauth: prefix - generated by twitchapps.com/tmi
        /// </summary>
        public string BotOAuthWithPrefix => "oauth:" + BotIrcOAuth;

        /// <summary>
        /// Bot's oauth string without oauth prefix
        /// </summary>
        public string BotOAuth { get; set; }

        public string BotIrcOAuth { get; set; }

        /// <summary>
        /// Bot's Client ID
        /// </summary>
        public string ClientID { get; set; }

        /// <summary>
        /// Name of channel where bot should join in lower case
        /// </summary>
        public string Channel { get; set; }

        public long ChannelId { get; set; }

        /// <summary>
        /// Name of the bot (username of bot's Twitch account) in lower case!
        /// </summary>
        public string BotName { get; set; }

        /// <summary>
        /// Full name of bot's twitch account
        /// </summary>
        public string BotFullTwitchName { get; set; } = String.Empty;

        /// <summary>
        /// If login credentials are valid
        /// </summary>
        public bool IsLoginCredentialsValid =>
                        !string.IsNullOrEmpty(BotName) &&
                        !string.IsNullOrEmpty(BotOAuthWithPrefix) &&
                        !string.IsNullOrEmpty(Channel);

        public void VerifyLoginCredentials()
        {
            if (!IsLoginCredentialsValid)
            {
                throw new ArgumentException("Login credentials are not valid!");
            }

            // These should be always lowercase
            BotName = BotName.ToLower();
            Channel = Channel.ToLower();
        }
    }
}
