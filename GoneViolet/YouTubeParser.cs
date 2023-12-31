﻿using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Object;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace GoneViolet
{
    public class YouTubeParser : IYouTubeParser
    {
        public string ParseVideo(string content)
        {
            string url = string.Empty;
            string quality = string.Empty;
            MatchCollection matches = Regex.Matches(
                content,
                @"<\s*(script)[^>]*?(?!=/)>(.*?)</\s*(\1)[^>]*?>",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(200));
            foreach (Match match in matches.Cast<Match>())
            {
                string decodedScript = HttpUtility.HtmlDecode(match.Groups[2].Value);
                if (!string.IsNullOrEmpty(match.Groups[2].Value)
                    && Regex.IsMatch(
                        decodedScript,
                        @"^\s*var\s*ytInitialPlayerResponse\b",
                        RegexOptions.IgnoreCase,
                        TimeSpan.FromMilliseconds(250))
                && Regex.IsMatch(
                    decodedScript,
                    @"\w+\.googlevideo\.com/videoplayback",
                    RegexOptions.IgnoreCase,
                    TimeSpan.FromMilliseconds(250)))
                {
                    //_logger.LogDebug(decodedScript);
                    JsValue formats = new Engine()
                        .Execute(decodedScript)
                        .Execute("var formats = ytInitialPlayerResponse.streamingData.formats;")
                        .GetValue("formats");

                    if (formats != null && formats.IsArray())
                    {
                        ArrayInstance formatsArray = formats.AsArray();
                        for (int i = 0; i < formatsArray.GetLength(); i += 1)
                        {
                            ObjectInstance format = formatsArray.Get(i.ToString(CultureInfo.InvariantCulture)).AsObject();
                            if (string.IsNullOrEmpty(quality) || string.Equals(format.Get("quality").AsString(), "hd720", StringComparison.OrdinalIgnoreCase))
                            {
                                quality = format.Get("quality").AsString();
                                url = format.Get("url").AsString();
                            }
                        }
                    }
                }
            }
            return url;
        }
    }
}
