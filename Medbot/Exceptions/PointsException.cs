using System;

namespace Medbot.Exceptions
{
    public class PointsException : Exception
    {
        /// <summary>
        /// Initializes a new PointsCommndException instance
        /// </summary>
        public PointsException() { }

        /// <summary>
        /// Initializes a new PointsException instance with specific error message
        /// </summary>
        /// <param name="message">Message that describes the error</param>
        public PointsException(string message) : base(message) { }
    }
}
