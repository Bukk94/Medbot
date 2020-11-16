using System;

namespace Medbot.Internal
{
    public static class InternalExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static bool ContainsInsensitive(this string source, string toCheck)
        {
            return Contains(source, toCheck, StringComparison.OrdinalIgnoreCase);
        }
    }
}
