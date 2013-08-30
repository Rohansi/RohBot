using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace SteamMobile
{
    class LinkTitles
    {
        const string ApiKey = "AIzaSyB2tZ7wquAcn3W78aqaaYKGVfIQWuuVNgg";

        public static string Lookup(string message)
        {
            var sb = new StringBuilder();
            var titles = LookupYoutube(message).Concat(LookupSpotify(message)).OrderBy(i => i.Item1);

            foreach (var i in titles)
            {
                if (!string.IsNullOrWhiteSpace(i.Item2))
                    sb.AppendLine(i.Item2);
            }

            return sb.ToString();
        }

        private static Regex spotify = new Regex(@"(http|https):\/\/\w*?.spotify.com\/track\/([\w]+)", RegexOptions.Compiled);
        private static IEnumerable<Tuple<int, string>> LookupSpotify(string message)
        {
            var matches = spotify.Matches(message);

            foreach (Match match in matches)
            {
                var offset = match.Index;
                var response = "Spotify: Error";

                try
                {
                    var spotifyResponse = DownloadPage(string.Format("http://ws.spotify.com/lookup/1/.json?uri={0}", HttpUtility.UrlEncode(match.Value)));

                    var token = JObject.Parse(spotifyResponse);
                    var track = token["track"];

                    var name = track["name"].ToObject<string>();
                    var artist = track["artists"].First["name"].ToObject<string>();
                    var length = track["length"].ToObject<double>();
                    var popularity = track["popularity"].ToObject<string>();

                    var formattedlength = FormatTime(TimeSpan.FromSeconds(length));

                    var numStars = (int)Math.Round(Convert.ToDouble(popularity) / 0.2);
                    var stars = new string('★', numStars).PadRight(5, '☆');

                    var chatResponse = string.Format("{0} - {1} ({2}) [{3}]", name, artist, formattedlength, stars);

                    var ytName = HttpUtility.UrlEncode(name);
                    var ytArtist = HttpUtility.UrlEncode(artist);

                    string youtubeUrl = null;
                    try
                    {
                        var apiQuery = string.Format("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&order=relevance&q={0}%20%2B%20{1}&key={2}", ytName, ytArtist, ApiKey);
                        var ytResponse = DownloadPage(apiQuery);

                        var ytToken = JObject.Parse(ytResponse);
                        youtubeUrl = ytToken["items"].First["id"]["videoId"].ToObject<string>();
                    }
                    catch { }

                    response = string.Format("Spotify: {0}{1}{2}", chatResponse, youtubeUrl != null ? " -> http://youtu.be/" : "", youtubeUrl);
                }
                catch { }

                yield return Tuple.Create(offset, response);
            }
        }

        private static Regex youtube = new Regex(@"youtube\.com/.*?(?:&|\?)v=([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        private static Regex youtubeShort = new Regex(@"youtu\.be/([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        private static IEnumerable<Tuple<int, string>> LookupYoutube(string message)
        {
            var matches = youtube.Matches(message).Cast<Match>();
            matches = matches.Concat(youtubeShort.Matches(message).Cast<Match>());

            foreach (Match match in matches)
            {
                var offset = match.Index;
                var response = "YouTube: Error";

                try
                {
                    var apiRequestUrl = string.Format(@"http://gdata.youtube.com/feeds/api/videos/{0}?alt=json", match.Groups[1].Value);
                    var responseFromServer = DownloadPage(apiRequestUrl);

                    var token = JObject.Parse(responseFromServer);
                    var name = token["entry"]["title"]["$t"].ToObject<string>();
                    var length = token["entry"]["media$group"]["media$content"].First()["duration"].ToObject<int>();
                    var formattedlength = FormatTime(TimeSpan.FromSeconds(length));

                    var stars = "";
                    try
                    {
                        var numStars = Math.Round(token["entry"]["gd$rating"]["average"].ToObject<double>());
                        stars = string.Format(" [{0}]", new string('★', (int)numStars).PadRight(5, '☆'));
                    } catch { }

                    response = string.Format("YouTube: {0} ({1}){2}", name, formattedlength, stars);
                } catch { }

                yield return Tuple.Create(offset, response);
            }
        }

        private static string DownloadPage(string uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.KeepAlive = true;
            request.Timeout = 5000;

            using (var response = request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        private static string FormatTime(TimeSpan time)
        {
            var format = @"m\:ss";
            if (time.Hours > 0)
                format = @"h\:m" + format;
            return time.ToString(format);
        }

        static LinkTitles()
        {
#pragma warning disable 612,618
            ServicePointManager.CertificatePolicy = new FuckSecurity();
#pragma warning restore 612,618
        }
    }

    public class FuckSecurity : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint sp, X509Certificate certificate, WebRequest request, int error)
        {
            return true;
        }
    }
}
