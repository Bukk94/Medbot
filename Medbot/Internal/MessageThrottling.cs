using Medbot.Events;
using Medbot.LoggingNS;
using System;
using System.Timers;

namespace Medbot.Internal
{
    public enum ThrottleViolation { MessageEmpty, MessageLimitExceeded, ExcessiveSending }

    internal class MessageThrottling
    {
        private Timer throttlingTimer;
        private int messagesSent;

        /// <summary>
        /// Activates when message is throttled
        /// </summary>
        internal event EventHandler<OnMessageThrottledArgs> OnMessageThrottled;

        /// <summary>
        /// Gets maximum character limit to be send
        /// </summary>
        internal int MaxCharacterLimit { get { return 500; } }

        /// <summary>
        /// Gets number of maximum messages to be send via bot in given Throttling Interval
        /// </summary>
        internal int MaxMessagesLimit { get { return 20; } }

        /// <summary>
        /// Gets number of maximum messages to be send via bot with moderator's permissions in given Throttling Interval
        /// </summary>
        internal int MaxModeratorMessagesLimit { get { return 100; } }

        /// <summary>
        /// Gets throttling interval
        /// </summary>
        internal TimeSpan ThrottlingInterval { get { return new TimeSpan(0, 0, 20); } }

        /// <summary>
        /// Creates an instance of Message throttler which can check if the message is allowed to be send
        /// </summary>
        internal MessageThrottling()
        {
            throttlingTimer = new Timer();
            messagesSent = 0;
            throttlingTimer.Interval = ThrottlingInterval.TotalMilliseconds;
            throttlingTimer.Elapsed += ThrottlingTimer_Tick;
        }

        /// <summary>
        /// Determines if the message is allowed to be sent via bot
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Bool if message is allowed to send</returns>
        internal bool AllowToSendMessage(string message)
        {
            if (!throttlingTimer.Enabled)
                throttlingTimer.Start();

            // Message is empty or exceeds maximum characater limit
            if (String.IsNullOrEmpty(message) || message.Length >= MaxCharacterLimit)
            {
                Logging.LogEvent(System.Reflection.MethodBase.GetCurrentMethod(), "Message was THROTTLED. Message was empty or exceeds maximum character limit: " + message);

                OnMessageThrottled?.Invoke(this, new OnMessageThrottledArgs
                {
                    Interval = ThrottlingInterval,
                    Message = message,
                    Violation = String.IsNullOrEmpty(message) ? ThrottleViolation.MessageEmpty : ThrottleViolation.MessageLimitExceeded
                });
                return false;
            }

            // Check messages limit
            if ((BotClient.BotModeratorPermission && messagesSent >= MaxModeratorMessagesLimit) || (!BotClient.BotModeratorPermission && messagesSent >= MaxMessagesLimit))
            {
                Logging.LogEvent(System.Reflection.MethodBase.GetCurrentMethod(), "Message was THROTTLED: " + message);
                OnMessageThrottled?.Invoke(this, new OnMessageThrottledArgs
                {
                    Interval = ThrottlingInterval,
                    Message = message,
                    Violation = ThrottleViolation.ExcessiveSending
                });
                return false;
            }

            messagesSent++;
            return true;
        }

        /// <summary>
        /// Throttling timer tick method, resetting timer and throttler
        /// </summary>
        void ThrottlingTimer_Tick(object sender, ElapsedEventArgs e)
        {
            throttlingTimer.Stop();
            messagesSent = 0;
        }
    }
}
