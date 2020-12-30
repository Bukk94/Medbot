using Microsoft.Extensions.Logging;

namespace Medbot
{
    internal static class Logging
    {
        internal static ILogger GetLogger<T>()
        {
            return LoggerFactoryExtensions.CreateLogger<T>(new NLog.Extensions.Logging.NLogLoggerFactory());
        }
    }
}
