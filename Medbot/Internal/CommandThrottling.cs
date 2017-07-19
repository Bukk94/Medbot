﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Medbot.Internal {
    internal class CommandThrottling {
        private Timer throttlingTimer;
        private TimeSpan throttlingInterval;
        private bool noCooldown;
        private bool cooldownActive;

        /// <summary>
        /// Gets throttling interval
        /// </summary>
        internal TimeSpan ThrottlingInterval { get { return this.throttlingInterval; } }

        /// <summary>
        /// Creates an instance of Command throttler which can check if the command is allowed to be executed
        /// </summary>
        internal CommandThrottling(TimeSpan interval) {
            throttlingInterval = interval;

            if (interval == null || interval.TotalMilliseconds <= 0) {
                this.noCooldown = true;
                return;
            }

            this.noCooldown = false;
            this.cooldownActive = false;
            throttlingTimer = new Timer();
            throttlingTimer.Interval = interval.TotalMilliseconds;
            throttlingTimer.Elapsed += ThrottlingTimer_Tick;
        }

        /// <summary>
        /// Determines if the command is allowed to be executed
        /// </summary>
        /// <returns>Bool if command is allowed to execute</returns>
        internal bool AllowedToExecute() {
            if (this.noCooldown)
                return true;

            if (!throttlingTimer.Enabled)
                throttlingTimer.Start();

            if (this.cooldownActive)
                return false;

            this.cooldownActive = true;
            return true;
        }

        /// <summary>
        /// Resets command's cooldown
        /// </summary>
        internal void ThrottlingTimer_Tick(object sender, ElapsedEventArgs e) {
            throttlingTimer.Stop();
            this.cooldownActive = false;
        }

        /// <summary>
        /// Resets throttling timer and bool throttler
        /// </summary>
        internal void ResetThrottlingTimer() {
            this.cooldownActive = false;

            if (throttlingTimer != null)
                throttlingTimer.Stop();
        }
    }
}