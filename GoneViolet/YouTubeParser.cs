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
    public class YouTubeParser : IYouTubeParser
    {
        private readonly ISignatureCipherDecoder _signatureCipherDecoder;

        public YouTubeParser(ISignatureCipherDecoder signatureCipherDecoder)
        {
            _signatureCipherDecoder = signatureCipherDecoder;
        }

        public async Task<string> ParseVideo(string content)
        {
            string url = string.Empty;
            string quality = string.Empty;
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
                    JsValue formats = new Engine()
                        .Execute(decodedScript)
                        .Execute("var formats = ytInitialPlayerResponse.streamingData.formats;")
                        .GetValue("formats");

                    if (formats != null && formats.IsArray())
                    {
                        ArrayInstance formatsArray = formats.AsArray();
                        for (int i = 0; i < formatsArray.Count(); i += 1)
                        {
                            ObjectInstance format = formatsArray.Get(i.ToString(CultureInfo.InvariantCulture)).AsObject();
                            if (string.IsNullOrEmpty(quality) || string.Equals(format.Get("quality").AsString(), "hd720", StringComparison.OrdinalIgnoreCase))
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
                // s=AJfAJfQdSswRQIhALg8HzdAybY2YJzDbCQeqt4Y2%3Diu3lBuuDJQph8nn47IAiAPL89mUkGvbQbbAtrmKtaFri6R2q-dHFSrcga8JQX5sQ%3DF%3DFF&sp=sig&url=https://rr4---sn-vgqsrn6z.googlevideo.com/videoplayback%3Fexpire%3D1710466609%26ei%3D0VHzZfKIC-bPir4Puri2gAQ%26ip%3D69.131.84.157%26id%3Do-AMVENnm2DGc2emz9hvWfSOtYxjVdpCrwTjy1JjWp38Tx%26itag%3D18%26source%3Dyoutube%26requiressl%3Dyes%26xpc%3DEgVo2aDSNQ%253D%253D%26mh%3Dun%26mm%3D31%252C29%26mn%3Dsn-vgqsrn6z%252Csn-vgqskn6d%26ms%3Dau%252Crdu%26mv%3Dm%26mvi%3D4%26pl%3D20%26gcr%3Dus%26initcwndbps%3D792500%26spc%3DUWF9fwR3plCG9sHsKKhA8B43mDThghYpjMTby_wSZkgWmxU%26vprv%3D1%26svpuc%3D1%26mime%3Dvideo%252Fmp4%26ns%3DKgr8UUNmURVCCVahDqzrBaYQ%26cnr%3D14%26ratebypass%3Dyes%26dur%3D148.143%26lmt%3D1688670787528071%26mt%3D1710444557%26fvip%3D4%26c%3DWEB%26sefc%3D1%26txp%3D1438434%26n%3DgVKIcItl4NaWpmHyK%26sparams%3Dexpire%252Cei%252Cip%252Cid%252Citag%252Csource%252Crequiressl%252Cxpc%252Cgcr%252Cspc%252Cvprv%252Csvpuc%252Cmime%252Cns%252Ccnr%252Cratebypass%252Cdur%252Clmt%26lsparams%3Dmh%252Cmm%252Cmn%252Cms%252Cmv%252Cmvi%252Cpl%252Cinitcwndbps%26lsig%3DAPTiJQcwRQIhAJ1EowOfi3To0mCmG7MejhCkCZ6XBoY031v_vwOD-WJiAiA-QsQpIBGfJ5QwF_y0Ha1IDD2adopALCYRH5C4729tUQ%253D%253D

                // mine https://rr4---sn-vgqsrnld.googlevideo.com/videoplayback?expire=1710467064&ei=mFPzZfLOM47wir4P9vO7qAo&ip=69.131.84.157&id=o-AM5ZPSF13witOFkio1xr6BOmGqy6ShDCdOdI1ErWbdsc&itag=18&source=youtube&requiressl=yes&xpc=EgVo2aDSNQ%3D%3D&mh=YK&mm=31%2C26&mn=sn-vgqsrnld%2Csn-p5qlsnrl&ms=au%2Conr&mv=m&mvi=4&pl=20&gcr=us&initcwndbps=786250&spc=UWF9f5PVEa_Q6yKlsANKXYByMt2Wt_sFi8M7WQz9M9BCJTs&vprv=1&svpuc=1&mime=video%2Fmp4&ns=OgFpUuJlYDtQlbIs3BQ5ZuQQ&cnr=14&ratebypass=yes&dur=3312.721&lmt=1697411958195109&mt=1710445271&fvip=2&c=WEB&sefc=1&txp=1438434&n=O-D45BuYOybBWW-64m&sparams=expire%2Cei%2Cip%2Cid%2Citag%2Csource%2Crequiressl%2Cxpc%2Cgcr%2Cspc%2Cvprv%2Csvpuc%2Cmime%2Cns%2Ccnr%2Cratebypass%2Cdur%2Clmt&lsparams=mh%2Cmm%2Cmn%2Cms%2Cmv%2Cmvi%2Cpl%2Cinitcwndbps&lsig=APTiJQcwRQIgSsRO9GqTL4jOf7Ahxdg1FUVjuOcHFSTe_CIbarwCw8UCIQDmUxNM7oQklLv85wmXyTsGvdGT4Ir_4qyNJDZ40RTmEg%3D%3D
                //      https://rr4---sn-vgqsrn6z.googlevideo.com/videoplayback?expire=1710467426&ei=AlXzZcyfEeWdybgPwN2C6Ag&ip=69.131.84.157&id=o-AEJMZ8jb1MV1-Jc83r98QgdEdhTO7Z_kji5_oWotYZF2&itag=18&source=youtube&requiressl=yes&xpc=EgVo2aDSNQ%3D%3D&mh=un&mm=31%2C29&mn=sn-vgqsrn6z%2Csn-vgqskn6d&ms=au%2Crdu&mv=m&mvi=4&pl=20&gcr=us&initcwndbps=875000&spc=UWF9f9ekqGd_tKMp6PWd-Y5pWypB_hbccF1uaLnCs53--zs&vprv=1&svpuc=1&mime=video%2Fmp4&ns=FJKRrOB9b1Bna82XFVWJTTYQ&cnr=14&ratebypass=yes&dur=148.143&lmt=1688670787528071&mt=1710445511&fvip=4&beids=24350321&c=WEB&sefc=1&txp=1438434&n=JRXEr60UjK6SSKAu2-&sparams=expire%2Cei%2Cip%2Cid%2Citag%2Csource%2Crequiressl%2Cxpc%2Cgcr%2Cspc%2Cvprv%2Csvpuc%2Cmime%2Cns%2Ccnr%2Cratebypass%2Cdur%2Clmt&lsparams=mh%2Cmm%2Cmn%2Cms%2Cmv%2Cmvi%2Cpl%2Cinitcwndbps&lsig=APTiJQcwRgIhAPJIDNTSaAlsaeRWTgLF_iHLHgkLP6QhoCLJ5Mj8qpxKAiEAi9aDP2EILaCXmqBIIt93-irsJbzj4QFLGyqMu046urM%3D&sig=NTNTIE0skm8HPH3vEzroURNy81VBuizkZLMyTuNCNLR_DICwFeddadY_yd5eWB8ZvWZewVM6s7gHd4CoL4zXKcbmAUgIARwsSdQfJAW
                // vlc  https://rr4---sn-vgqskn6d.googlevideo.com/videoplayback?expire=1710466159&ei=D1DzZcvzGd-Hlu8P79uGwAs&ip=69.131.84.157&id=o-AKRG7SYmYzBl9pb09P7yXIzlAvxef1VHnPZ-Ky0OkbRU&itag=22&source=youtube&requiressl=yes&xpc=EgVo2aDSNQ%3D%3D&mh=un&mm=31%2C26&mn=sn-vgqskn6d%2Csn-p5qlsn6l&ms=au%2Conr&mv=m&mvi=4&pl=20&gcr=us&initcwndbps=777500&spc=UWF9f76GdZtyXG9pAhH32L4ahBOX4wJsM7Ro_HUCGQT8Qn8&vprv=1&svpuc=1&mime=video%2Fmp4&ns=QOsNZeYXLpCDAFkUOTYykBgQ&cnr=14&ratebypass=yes&dur=148.143&lmt=1688670789895359&mt=1710444076&fvip=3&c=WEB&sefc=1&txp=1432434&n=LD4QYVhSF_qgYdehg&sparams=expire%2Cei%2Cip%2Cid%2Citag%2Csource%2Crequiressl%2Cxpc%2Cgcr%2Cspc%2Cvprv%2Csvpuc%2Cmime%2Cns%2Ccnr%2Cratebypass%2Cdur%2Clmt&lsparams=mh%2Cmm%2Cmn%2Cms%2Cmv%2Cmvi%2Cpl%2Cinitcwndbps&lsig=APTiJQcwRQIhAJ4a_5LhBcwRPBIkpnqbdLB2OX53t1cudFfEeYKHO6yFAiAp-rSdBRAZY7WX8n17Z8dsJww3j9vaOj9ZDRLJUgroqQ%3D%3D&sig=AJfQdSswRgIhAMojgA6cuWDRRcRe6oQKLiPp5qOsQdjt60dlAfMUFBnCAiEAtiMXygKPdH1LMO7CzSUOPtDLVzZO3yu61Qpc2gs6JUU%3D
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
