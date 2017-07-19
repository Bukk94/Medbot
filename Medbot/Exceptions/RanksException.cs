using System;

namespace Medbot.Exceptions {
    public class RanksException : Exception {

        /// <summary>
        /// Initializes a new PointsCommndException instance
        /// </summary>
        public RanksException() { }

        /// <summary>
        /// Initializes a new PointsException instance with specific error message
        /// </summary>
        /// <param name="message">Message that describes the error</param>
        public RanksException(string message) : base(message) { }

    }
}
