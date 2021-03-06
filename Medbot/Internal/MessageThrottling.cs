﻿using Medbot.Events;
using Medbot.Enums;
using System;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace Medbot.Internal
{
    internal class MessageThrottling
    {
        private readonly ILogger _logger;
        private readonly BotDataManager _botDataManager;
        private readonly Timer _throttlingTimer;
        private int _messagesSent;

        /// <summary>
        /// Activates when message is throttled
        /// </summary>
        internal event EventHandler<OnMessageThrottledArgs> OnMessageThrottled;

        /// <summary>
        /// Gets maximum character limit to be send
        /// </summary>
        internal int MaxCharacterLimit => 500;

        /// <summary>
        /// Gets number of maximum messages to be send via bot in given Throttling Interval
        /// </summary>
        internal int MaxMessagesLimit => 20;

        /// <summary>
        /// Gets number of maximum messages to be send via bot with moderator's permissions in given Throttling Interval
        /// </summary>
        internal int MaxModeratorMessagesLimit => 100;

        /// <summary>
        /// Gets throttling interval
        /// </summary>
        internal TimeSpan ThrottlingInterval => TimeSpan.FromSeconds(20);

        /// <summary>
        /// Creates an instance of Message throttler which can check if the message is allowed to be send
        /// </summary>
        internal MessageThrottling(BotDataManager botDataManager)
        {
            _logger = Logging.GetLogger<MessageThrottling>();

            _botDataManager = botDataManager;
            _throttlingTimer = new Timer();
            _messagesSent = 0;
            _throttlingTimer.Interval = ThrottlingInterval.TotalMilliseconds;
            _throttlingTimer.Elapsed += ThrottlingTimer_Tick;
        }

        /// <summary>
        /// Determines if the message is allowed to be sent via bot
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Bool if message is allowed to send</returns>
        internal bool AllowToSendMessage(string message)
        {
            if (!_throttlingTimer.Enabled)
                _throttlingTimer.Start();

            // Message is empty or exceeds maximum characater limit
            if (String.IsNullOrEmpty(message) || message.Length >= MaxCharacterLimit)
            {
                _logger.LogInformation("Message was THROTTLED. Message was empty or exceeds maximum character limit: {message}.", message);

                OnMessageThrottled?.Invoke(this, new OnMessageThrottledArgs
                {
                    Interval = ThrottlingInterval,
                    Message = message,
                    Violation = String.IsNullOrEmpty(message) ? ThrottleViolation.MessageEmpty : ThrottleViolation.MessageLimitExceeded
                });
                return false;
            }

            // Check messages limit
            if ((_botDataManager.IsBotModerator && _messagesSent >= MaxModeratorMessagesLimit) || (!_botDataManager.IsBotModerator && _messagesSent >= MaxMessagesLimit))
            {
                _logger.LogInformation("Message was THROTTLED: {message}.", message);

                OnMessageThrottled?.Invoke(this, new OnMessageThrottledArgs
                {
                    Interval = ThrottlingInterval,
                    Message = message,
                    Violation = ThrottleViolation.ExcessiveSending
                });
                return false;
            }

            _messagesSent++;
            return true;
        }

        /// <summary>
        /// Throttling timer tick method, resetting timer and throttler
        /// </summary>
        void ThrottlingTimer_Tick(object sender, ElapsedEventArgs e)
        {
            _throttlingTimer.Stop();
            _messagesSent = 0;
        }
    }
}
