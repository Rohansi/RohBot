using System;
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
            // TODO: handle multiple URLs in messages

            var sb = new StringBuilder();

            try
            {
                var spot = LookupSpotify(message);
                if (!string.IsNullOrWhiteSpace(spot))
                    sb.AppendLine(spot);
            }
            catch { }

            try
            {
                var yt = LookupYoutube(message);
                if (!string.IsNullOrWhiteSpace(yt))
                    sb.AppendLine(yt);
            } catch { }

            return sb.ToString();
        }

        private static string LookupSpotify(string message)
        {
            var match = Regex.Match(message, @"(http|https):\/\/.*.spotify.com\/track\/([\w]+)");
            if (!match.Success)
                return null;

            var spotifyResponse = DownloadPage(string.Format("http://ws.spotify.com/lookup/1/.json?uri={0}", match.Value));

            var token = JObject.Parse(spotifyResponse);
            var track = token.SelectToken("track");

            var name = track.SelectToken("name").ToObject<string>();
            var artist = track.SelectToken("artists").First.SelectToken("name").ToObject<string>();
            var length = track.SelectToken("length").ToObject<double>();
            var popularity = track.SelectToken("popularity").ToObject<string>();

            var formattedlength = TimeSpan.FromSeconds(length).ToString(@"mm\:ss");

            var numStars = (int)Math.Round(Convert.ToDouble(popularity) / 0.2);
            var stars = new String('★', numStars).PadRight(5, '☆');

            var chatResponse = string.Format("{0} - {1} ({2}) [{3}]", name, artist, formattedlength, stars);

            var ytName = Uri.EscapeDataString(name);
            var ytArtist = Uri.EscapeDataString(artist);

            string youtubeUrl = null;
            try
            {
                var apiQuery = string.Format(@"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&order=relevance&q=" + ytName + "%20%2B%20" + ytArtist + "&key=" + ApiKey);
                var ytResponse = DownloadPage(apiQuery);

                var ytToken = JObject.Parse(ytResponse);
                youtubeUrl = ytToken.SelectToken("items").First.SelectToken("id").SelectToken("videoId").ToObject<string>();
            }
            catch (Exception e) { Console.WriteLine(e); }

            return string.Format("{0}{1}{2}", chatResponse, youtubeUrl != null ? " -> http://youtu.be/" : "", youtubeUrl);
        }

        private static string LookupYoutube(string message)
        {
            var youtubeRegex = Regex.Match(message, @"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)");

            if (!youtubeRegex.Success)
                return null;

            var apiRequestUrl = string.Format(@"http://gdata.youtube.com/feeds/api/videos/{0}?alt=json&fields=title", youtubeRegex.Groups[1].Value);
            var responseFromServer = DownloadPage(apiRequestUrl);

            var token = JObject.Parse(responseFromServer);
            var name = (string)token["entry"]["title"]["$t"];
            return string.Format("Video Title: {0}", name);
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
    }
}
