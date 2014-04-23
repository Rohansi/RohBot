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
        public static string Lookup(string message)
        {
            var sb = new StringBuilder();
            var titles = LookupYoutube(message)
                        .Concat(LookupSpotify(message))
                        .Concat(LookupFacepunch(message))
                        .OrderBy(i => i.Item1)
                        .Where(i => !string.IsNullOrWhiteSpace(i.Item2.Value))
                        .Take(5);

            foreach (var i in titles)
            {
                sb.AppendLine(i.Item2.Value);
            }

            var res = sb.ToString().TrimEnd();
            if (res.Length > 500)
                res = res.Substring(0, 500) + "...";
            return res;
        }

        private static Regex _spotify = new Regex(@"https?://\w*?.spotify.com/track/([\w]+)", RegexOptions.Compiled);
        private static IEnumerable<Tuple<int, Lazy<string>>> LookupSpotify(string message)
        {
            var matches = _spotify.Matches(message).Cast<Match>();

            foreach (Match m in matches.DistinctBy(m => m.Value))
            {
                var match = m;
                var offset = match.Index;
                var response = new Lazy<string>(() =>
                {
                    try
                    {
                        var url = string.Format("http://ws.spotify.com/lookup/1/.json?uri={0}", HttpUtility.UrlEncode(match.Value));
                        var spotifyResponse = DownloadPage(url, "UTF-8");

                        var token = JObject.Parse(spotifyResponse);
                        var track = token["track"];

                        var name = track["name"].ToObject<string>();
                        var artist = track["artists"].First["name"].ToObject<string>();
                        var length = track["length"].ToObject<double>();

                        var formattedlength = FormatTime(TimeSpan.FromSeconds(length));

                        var chatResponse = string.Format("{0} - {1} ({2})", name, artist, formattedlength);

                        var ytName = HttpUtility.UrlEncode(name);
                        var ytArtist = HttpUtility.UrlEncode(artist);

                        /*
                         * Spotify2YT included with permission of glorious god-king Naarkie
                         */
                        string youtubeUrl = null;
                        try
                        {
                            const string apiKey = "AIzaSyB2tZ7wquAcn3W78aqaaYKGVfIQWuuVNgg";
                            var apiQuery = string.Format("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&order=relevance&q={0}%20%2B%20{1}&key={2}", ytName, ytArtist, apiKey);
                            var ytResponse = DownloadPage(apiQuery, "UTF-8");

                            var ytToken = JObject.Parse(ytResponse);
                            youtubeUrl = ytToken["items"].First["id"]["videoId"].ToObject<string>();
                        }
                        catch { }

                        return string.Format("Spotify: {0}{1}{2}", chatResponse, youtubeUrl != null ? " -> http://youtu.be/" : "", youtubeUrl);
                    }
                    catch (Exception e)
                    {
                        Program.Logger.Warn("LinkTitles Error", e);
                    }

                    return null;
                });

                yield return Tuple.Create(offset, response);
            }
        }

        private static Regex _youtube = new Regex(@"youtube\.com/watch\S*?(?:&v|\?v)=([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        private static Regex _youtubeShort = new Regex(@"youtu\.be/([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        private static IEnumerable<Tuple<int, Lazy<string>>> LookupYoutube(string message)
        {
            var matches = _youtube.Matches(message).Cast<Match>();
            matches = matches.Concat(_youtubeShort.Matches(message).Cast<Match>());

            foreach (Match m in matches.DistinctBy(m => m.Groups[1].Value))
            {
                var match = m;
                var offset = match.Index;
                var response = new Lazy<string>(() =>
                {
                    try
                    {
                        var videoId = match.Groups[1].Value;

                        // youtube video ids are 11 characters and will ignore extra characters
                        // the api however does not
                        if (videoId.Length > 11)
                            videoId = videoId.Substring(0, 11);

                        var apiRequestUrl = string.Format(@"http://gdata.youtube.com/feeds/api/videos/{0}?alt=json", videoId);
                        var responseFromServer = DownloadPage(apiRequestUrl, "UTF-8");

                        var token = JObject.Parse(responseFromServer);
                        var name = token["entry"]["title"]["$t"].ToObject<string>();
                        var length = token["entry"]["media$group"]["yt$duration"]["seconds"].ToObject<int>();
                        var formattedlength = FormatTime(TimeSpan.FromSeconds(length));

                        var stars = "";
                        try
                        {
                            var numStars = Math.Round(token["entry"]["gd$rating"]["average"].ToObject<double>());
                            stars = string.Format(" [{0}]", new string('★', (int)numStars).PadRight(5, '☆'));
                        }
                        catch { }

                        return string.Format("YouTube: {0} ({1}){2}", name, formattedlength, stars);
                    }
                    catch (Exception e)
                    {
                        Program.Logger.Warn("LinkTitles Error", e);
                    }

                    return null;
                });

                yield return Tuple.Create(offset, response);
            }
        }

        private static Regex _facepunch = new Regex(@"http://facepunch\.com/showthread\.php\S*?(?:&[tp]|\?[tp])=\d+", RegexOptions.Compiled);
        private static Regex _facepunchTitle = new Regex(@"<title\b[^>]*>(.*?)</title>", RegexOptions.Compiled);
        private static IEnumerable<Tuple<int, Lazy<string>>> LookupFacepunch(string message)
        {
            var matches = _facepunch.Matches(message).Cast<Match>();

            foreach (Match m in matches.DistinctBy(m => m.Value))
            {
                var match = m;
                var offset = match.Index;
                var response = new Lazy<string>(() =>
                {
                    try
                    {
                        var page = DownloadPage(match.Value, "Windows-1252");
                        var title = WebUtility.HtmlDecode(_facepunchTitle.Match(page).Groups[1].Value.Trim());

                        if (title == "Facepunch")
                            return null;

                        return string.Format("Facepunch: {0}", title);
                    }
                    catch (Exception e)
                    {
                        Program.Logger.Warn("LinkTitles Error", e);
                    }

                    return null;
                });

                yield return Tuple.Create(offset, response);
            }
        }

        private static string DownloadPage(string uri, string encoding)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.KeepAlive = true;
            request.Timeout = 5000;

            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding)))
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
