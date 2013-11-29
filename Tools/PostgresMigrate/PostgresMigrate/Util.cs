using System.Collections.Generic;
using System.Text;

namespace PostgresMigrate
{
    public static class Util
    {
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
