using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile
{
    public static class Util
    {
        // http://stackoverflow.com/a/7983514
        private static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetCurrentUnixTimestamp()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
        }

        public static DateTime DateTimeFromUnixTimestamp(long seconds)
        {
            return UnixEpoch.AddSeconds(seconds);
        }
    }
}
