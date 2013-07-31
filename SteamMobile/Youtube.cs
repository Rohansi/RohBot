using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace SteamMobile
{

    class Youtube
    {
        public static string LookupYoutube(string youtube)
        {
            var youtubeRegex = Regex.Match(youtube, @"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)");

            if (youtubeRegex.Success)
            {
                var apiRequestUrl = string.Format(
                    @"http://gdata.youtube.com/feeds/api/videos/{0}?alt=json&fields=title",
                    youtubeRegex.Groups[1].Value);

                var httpResponse = (HttpWebResponse)WebRequest.Create(apiRequestUrl).GetResponse();
                var responseFromServer = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();

                var token = JObject.Parse(responseFromServer);

                var name = (string)token["entry"]["title"]["$t"];
                return string.Format("Video Title: {0}", name);
            }
            return null;
        }
    }
}
