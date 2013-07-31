using System;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace SteamMobile
{

    class Spotify
    {
        public static string LookupYoutube(string message)
        {
            var match = Regex.Match(message, @"(http|https):\/\/.*.spotify.com\/track\/([\w]+)");
            if (match.Success)
            {
                var httpRequest =
                    (HttpWebRequest)
                    WebRequest.Create(string.Format("http://ws.spotify.com/lookup/1/.json?uri={0}", match.Value));
                httpRequest.Method = "GET";
                httpRequest.KeepAlive = false;
                httpRequest.ContentType = "text/json";

                using (var httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream(), System.Text.Encoding.UTF8))
                {
                    var token = JObject.Parse(streamReader.ReadToEnd());
                    var track = token.SelectToken("track");

                    var name = track.SelectToken("name").ToObject<string>();
                    var artist = track.SelectToken("artists").First.SelectToken("name").ToObject<string>();
                    var length = track.SelectToken("length").ToObject<double>();
                    var popularity = track.SelectToken("popularity").ToObject<string>();

                    var formattedlength = TimeSpan.FromSeconds(length).ToString(@"mm\:ss");

                    var numStars = (int)Math.Round(Convert.ToDouble(popularity) / 0.2);
                    var stars = new String('★', numStars).PadRight(5, '☆');

                    var chatResponse = string.Format("{0} - {1} ({2}) [{3}]", name, artist, formattedlength, stars);

                    const string Apikey = "AIzaSyB2tZ7wquAcn3W78aqaaYKGVfIQWuuVNgg";
                    var _name = Uri.EscapeDataString(name);
                    var _artist = Uri.EscapeDataString(artist);

                    var apiQuery =
                        @"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&order=relevance&q="
                        + _name + "%20%2B%20" + _artist + "&key=" + Apikey;
                    var ytResponse = (HttpWebResponse)WebRequest.Create(apiQuery).GetResponse();
                    var responseFromServer = new StreamReader(ytResponse.GetResponseStream()).ReadToEnd();

                    var ytToken = JObject.Parse(responseFromServer);

                    var url =
                        ytToken.SelectToken("items").First.SelectToken("id").SelectToken("videoId").ToObject<string>();

                    ytResponse.Close();

                    return string.Format("{0} -> " + "http://youtu.be/{1}", chatResponse, url);


                }
            }
            return null;
        }
    }
}
