using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace RohBot
{
    public static class LinkTitles
    {
        const string ApiKey = "AIzaSyB2tZ7wquAcn3W78aqaaYKGVfIQWuuVNgg";

        public static async Task<string> Lookup(string message)
        {
            var sb = new StringBuilder();
            var titles = LookupYoutube(message)
                        .Concat(LookupSpotify(message))
                        .Concat(LookupFacepunch(message))
                        .OrderBy(i => i.Item1)
                        .Take(5)
                        .ToList();

            await Task.WhenAll(titles.Select(i => i.Item2.Value));

            foreach (var i in titles)
            {
                if (!string.IsNullOrWhiteSpace(i.Item2.Value.Result))
                    sb.AppendLine(i.Item2.Value.Result);
            }

            var res = sb.ToString().TrimEnd();
            if (res.Length > 500)
                res = res.Substring(0, 500) + "...";
            return res;
        }

        private static Regex _spotify = new Regex(@"https?://\w*?.spotify.com/track/([\w]+)", RegexOptions.Compiled);
        private static Regex _spotifyShort = new Regex(@"spotify:track:([\w]+)", RegexOptions.Compiled);
        private static IEnumerable<Tuple<int, AsyncLazy<string>>> LookupSpotify(string message)
        {
            var matches = _spotify.Matches(message).Cast<Match>();
            matches = matches.Concat(_spotifyShort.Matches(message).Cast<Match>());

            foreach (var m in matches.DistinctBy(m => m.Value))
            {
                var match = m;
                var offset = match.Index;
                var response = new AsyncLazy<string>(async () =>
                {
                    try
                    {
                        var url = string.Format("https://api.spotify.com/v1/tracks/{0}", HttpUtility.UrlEncode(match.Groups[1].Value));
                        var spotifyResponse = await DownloadPage(url, Encoding.UTF8);

                        var track = JObject.Parse(spotifyResponse);

                        var name = track["name"].ToObject<string>();
                        var artist = track["artists"].First["name"].ToObject<string>();
                        var length = track["duration_ms"].ToObject<int>();

                        var formattedLength = FormatTime(TimeSpan.FromMilliseconds(length));

                        var chatResponse = $"{name} - {artist} ({formattedLength})";

                        /*
                         * Spotify2YT included with permission of glorious god-king Naarkie
                         */
                        var ytName = HttpUtility.UrlEncode(name);
                        var ytArtist = HttpUtility.UrlEncode(artist);
                        string youtubeUrl = null;
                        try
                        {
                            var apiQuery = string.Format("https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&order=relevance&q={0}%20%2B%20{1}&key={2}", ytName, ytArtist, ApiKey);
                            var ytResponse = await DownloadPage(apiQuery, Encoding.UTF8);

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

        private static Regex _youtube = new Regex(@"youtube\.com/watch\S*?(?:&amp;|\?)v=([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        private static Regex _youtubeShort = new Regex(@"youtu\.be/([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        private static IEnumerable<Tuple<int, AsyncLazy<string>>> LookupYoutube(string message)
        {
            var matches = _youtube.Matches(message).Cast<Match>();
            matches = matches.Concat(_youtubeShort.Matches(message).Cast<Match>());

            foreach (var m in matches.DistinctBy(m => m.Groups[1].Value))
            {
                var match = m;
                var offset = match.Index;
                var response = new AsyncLazy<string>(async () =>
                {
                    try
                    {
                        var videoId = match.Groups[1].Value;

                        // youtube video ids are 11 characters and will ignore extra characters
                        // the api however does not
                        if (videoId.Length > 11)
                            videoId = videoId.Substring(0, 11);

                        var apiRequestUrl = string.Format(@"https://www.googleapis.com/youtube/v3/videos?part=snippet,contentDetails,statistics&id={0}&key={1}", videoId, ApiKey);
                        var responseFromServer = await DownloadPage(apiRequestUrl, Encoding.UTF8);

                        var token = JObject.Parse(responseFromServer);
                        var item = token["items"].First;
                        var title = item["snippet"]["title"].ToObject<string>();
                        var length = ParseDuration(item["contentDetails"]["duration"].ToObject<string>());
                        var formattedLength = FormatTime(TimeSpan.FromSeconds(length));

                        var statistics = item["statistics"];
                        var likeCount = statistics["likeCount"];
                        var dislikeCount = statistics["dislikeCount"];

                        var stats = string.Format("{0} • {1:n0} views",
                            formattedLength,
                            statistics["viewCount"].ToObject<long>()
                        );

                        if (likeCount != null && dislikeCount != null)
                        {
                            stats += $" • {likeCount.ToObject<int>():n0} 👍 {dislikeCount.ToObject<int>():n0} 👎";
                        }

                        return $"YouTube: {title} ({stats})";
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

        private static Regex _facepunch = new Regex(@"facepunch\.com/showthread\.php\S*?(?:&amp;|\?)t=(\d+)", RegexOptions.Compiled);
        private static Regex _facepunchTitle = new Regex(@"<title\b[^>]*>(.*?)</title>", RegexOptions.Compiled);
        private static IEnumerable<Tuple<int, AsyncLazy<string>>> LookupFacepunch(string message)
        {
            var matches = _facepunch.Matches(message).Cast<Match>();

            foreach (var m in matches.DistinctBy(m => m.Value))
            {
                var match = m;
                var offset = match.Index;
                var response = new AsyncLazy<string>(async () =>
                {
                    try
                    {
                        var page = await DownloadPage("http://" + match.Value, Encoding.GetEncoding("Windows-1252"));
                        var title = WebUtility.HtmlDecode(_facepunchTitle.Match(page).Groups[1].Value.Trim());

                        if (title == "Facepunch")
                            return null;

                        return $"Facepunch: {title}";
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

        private async static Task<string> DownloadPage(string uri, Encoding encoding)
        {
            var client = new WebClient();

            var request = client.DownloadDataTaskAsync(uri);
            var timeout = Task.Delay(TimeSpan.FromSeconds(10));
            var completed = await Task.WhenAny(request, timeout);

            if (completed == timeout)
            {
                client.CancelAsync();
                throw new TimeoutException("DownloadPage timed out");
            }

            using (var reader = new StreamReader(new MemoryStream(request.Result), encoding))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static string FormatTime(TimeSpan time)
        {
            var format = @"m\:ss";
            if (time.Hours > 0)
                format = @"h\:m" + format;
            return time.ToString(format);
        }

        private static Regex _duration = new Regex(@"[0-9]+[HMS]", RegexOptions.Compiled);
        private static int ParseDuration(string duration)
        {
            var seconds = 0;

            foreach (var match in _duration.Matches(duration).Cast<Match>())
            {
                var value = match.Value;
                var unit = value[value.Length - 1];
                var amount = int.Parse(value.Substring(0, value.Length - 1));

                switch (unit)
                {
                    case 'H':
                        seconds += amount * 60 * 60;
                        break;
                    case 'M':
                        seconds += amount * 60;
                        break;
                    case 'S':
                        seconds += amount;
                        break;
                }
            }

            return seconds;
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
