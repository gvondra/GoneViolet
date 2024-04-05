using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Object;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace GoneViolet
{
    public class YouTubeHtmlParser : IYouTubeHtmlParser
    {
        private readonly ISignatureCipherDecoder _signatureCipherDecoder;

        public YouTubeHtmlParser(ISignatureCipherDecoder signatureCipherDecoder)
        {
            _signatureCipherDecoder = signatureCipherDecoder;
        }

        public Task<string> ParseVideo(string content)
            => InnerParseVideo(content, "hd720");

        public Task<string> ParseAudio(string content)
            => InnerParseVideo(content, "tiny", true, "^audio/mp4");

        private async Task<string> InnerParseVideo(
            string content,
            string targetQuality,
            bool exactQualityMatch = false,
            string mimeType = "")
        {
            string url = string.Empty;
            MatchCollection matches = Regex.Matches(
                content,
                @"<\s*(script)[^>]*?(?!=/)>(.*?)</\s*(\1)[^>]*?>",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(200));
            // itterate through all the script tags in the html
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
                    // load the javascript
                    JsValue formats;
                    Engine engine;
                    try
                    {
                        engine = new Engine()
                        .Execute(decodedScript)
                        .Execute("var formats = ytInitialPlayerResponse.streamingData.formats;")
                        .Execute("var adaptiveFormats = ytInitialPlayerResponse.streamingData.adaptiveFormats;");
                        formats = engine.GetValue("formats");
                    }
                    catch (Exception ex)
                    {
                        ApplicationException applicationException = new ApplicationException("Error processing JS", ex);
                        applicationException.Data["script"] = decodedScript;
                        throw applicationException;
                    }

                    url = await SearchFormats(formats, content, targetQuality, exactQualityMatch, mimeType);
                    if (string.IsNullOrEmpty(url))
                    {
                        formats = engine.GetValue("adaptiveFormats");
                        url = await SearchFormats(formats, content, targetQuality, exactQualityMatch, mimeType);
                    }
                }
            }
            return url;
        }

        private async Task<string> SearchFormats(
            JsValue formats,
            string content,
            string targetQuality,
            bool exactQualityMatch,
            string mimeTypePattern)
        {
            string url = string.Empty;
            string quality = string.Empty;
            if (formats != null && formats.IsArray())
            {
                ArrayInstance formatsArray = formats.AsArray();
                for (int i = 0; i < formatsArray.Count(); i += 1)
                {
                    ObjectInstance format = formatsArray.Get(i.ToString(CultureInfo.InvariantCulture)).AsObject();
                    if ((string.IsNullOrEmpty(quality) && !exactQualityMatch)
                        || (string.Equals(format.Get("quality").AsString(), targetQuality, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrEmpty(mimeTypePattern) || Regex.IsMatch(format.Get("mimeType").AsString(), mimeTypePattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500)))))
                    {
                        quality = format.Get("quality").AsString();
                        JsValue urlValue = format.Get("url");
                        if (!urlValue.IsUndefined())
                        {
                            url = urlValue.AsString();
                        }
                        else
                        {
                            url = await GetUrlFromSignatureCipher(content, format.Get("signatureCipher"));
                        }
                    }
                }
            }
            return url;
        }

        private async Task<string> GetUrlFromSignatureCipher(string content, JsValue signatureCipherValue)
        {
            string url = string.Empty;
            if (!signatureCipherValue.IsUndefined())
            {
                string signatureCipher = signatureCipherValue.AsString();
                NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(signatureCipher);
                IEnumerator keyEnumerator = nameValueCollection.Keys.GetEnumerator();
                string s = string.Empty;
                while (keyEnumerator.MoveNext())
                {
                    if (string.Equals("url", (string)keyEnumerator.Current, StringComparison.OrdinalIgnoreCase))
                    {
                        url = nameValueCollection[(string)keyEnumerator.Current];
                    }
                    else if (string.Equals("s", (string)keyEnumerator.Current, StringComparison.OrdinalIgnoreCase))
                    {
                        s = nameValueCollection[(string)keyEnumerator.Current];
                    }
                }
                if (!string.IsNullOrEmpty(s))
                {
                    url += "&sig=" + await _signatureCipherDecoder.Decode(content, s);
                }
            }
            return url;
        }

        public List<string> GetTags(string content)
        {
            List<string> result = new List<string>();
            MatchCollection matches = Regex.Matches(content, @"<meta\b[^>]*\""og:video:tag\""[^>]*/?>", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            foreach (Match match in matches.Cast<Match>())
            {
                Match tag = Regex.Match(match.Value, @"content\s*=\s*\""([^\""]+)\""", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
                if (tag != null && tag.Success)
                {
                    result.Add(HttpUtility.HtmlDecode(tag.Groups[1].Value));
                }
            }
            return result;
        }
    }
}
