using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SteamMobile
{
    public static class Util
    {
        #region Authentication
        public static byte[] HashPassword(string password, byte[] salt)
        {
            if (salt == null || salt.Length != 16)
                throw new Exception("bad salt");

            var h = new Rfc2898DeriveBytes(password, salt, 1000);
            return h.GetBytes(128);
        }

        private static Random _random = new Random();
        public static byte[] GenerateSalt()
        {
            var salt = new byte[16];
            _random.NextBytes(salt);
            return salt;
        }

        public static string GenerateLoginToken()
        {
            return Convert.ToBase64String(GenerateSalt());
        }

        public const string InvalidUsernameMessage = "Usernames must be between 2 and 24 characters long and may only contain letters, digits or spaces.";
        public static bool IsValidUsername(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            if (value.Length < 2 || value.Length > 24)
                return false;
            if (value.ToLower() == "guest" || value.ToLower() == "broadcast")
                return false;
            return value.All(c => char.IsLetterOrDigit(c) || c == ' ');
        }

        public const string InvalidPasswordMessage = "Passwords must be at least 6 characters long.";
        public static bool IsValidPassword(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            return value.Length >= 6;
        }
        #endregion

        #region Time
        // http://stackoverflow.com/a/7983514
        private static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetCurrentUnixTimestamp()
        {
            return GetUnixTimestamp(DateTime.UtcNow);
        }

        public static long GetUnixTimestamp(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - UnixEpoch).TotalSeconds;
        }

        public static DateTime DateTimeFromUnixTimestamp(long seconds)
        {
            return UnixEpoch.AddSeconds(seconds);
        }
        #endregion

        #region Misc
        // http://stackoverflow.com/a/654454/1056845
        public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dict,
                                     Func<KeyValuePair<TKey, TValue>, bool> condition)
        {
            foreach (var cur in dict.Where(condition).ToList())
            {
                dict.Remove(cur.Key);
            }
        }
        #endregion

        #region HtmlEncode
        // Replacement HtmlEncode because Mono's is broken
        public static string HtmlEncode(string value)
        {
            var result = new StringBuilder();
            foreach (var cp in value.AsCodePoints())
            {
                switch (cp)
                {
                    case 9: // TAB
                        result.Append('\t');
                        break;
                    case 10: // LF
                        result.Append('\n');
                        break;
                    case 13: // CR
                        result.Append('\r');
                        break;
                    case 34: // "
                        result.Append("&quot;");
                        break;
                    case 38: // &
                        result.Append("&amp;");
                        break;
                    case 39: // '
                        result.Append("&#39;");
                        break;
                    case 60: // <
                        result.Append("&lt;");
                        break;
                    case 62: // >
                        result.Append("&gt;");
                        break;
                    case 0xFF02: // Unicode "
                        result.Append("&#65282;");
                        break;
                    case 0xFF06: // Unicode &
                        result.Append("&#65286;");
                        break;
                    case 0xFF07: // Unicode '
                        result.Append("&#65287;");
                        break;
                    case 0xFF1C: // Unicode <
                        result.Append("&#65308;");
                        break;
                    case 0xFF1E: // Unicode >
                        result.Append("&#65310;");
                        break;

                    default:
                        if (cp <= 31 || (cp >= 127 && cp <= 159) || (cp >= 55296 && cp <= 57343))
                            break;

                        if (cp > 159 && cp < 256)
                        {
                            result.Append("&#");
                            result.Append(cp.ToString("D"));
                            result.Append(";");
                        }
                        else
                        {
                            result.Append(char.ConvertFromUtf32(cp));
                        }
                        break;
                }
            }
            return result.ToString();
        }

        private static IEnumerable<int> AsCodePoints(this string s)
        {
            for (int i = 0; i < s.Length; ++i)
            {
                yield return char.ConvertToUtf32(s, i);
                if (char.IsHighSurrogate(s, i))
                    i++;
            }
        }
        #endregion
    }
}
