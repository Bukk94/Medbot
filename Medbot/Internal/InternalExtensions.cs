using System;
using System.Collections.Generic;
using System.Linq;

namespace Medbot.Internal
{
    public static class InternalExtensions
    {
        private static readonly Random _random = new Random(Guid.NewGuid().GetHashCode());
             
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static bool ContainsInsensitive(this string source, string toCheck)
        {
            return Contains(source, toCheck, StringComparison.OrdinalIgnoreCase);
        }

        public static T SelectOneRandom<T>(this IEnumerable<T> array)
        {
            return array.ElementAt(_random.Next(0, array.Count()));
        }
    }
}
