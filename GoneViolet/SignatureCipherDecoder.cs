using Jint;
using Jint.Native;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoneViolet
{
    public class SignatureCipherDecoder : ISignatureCipherDecoder
    {
        private readonly IVideoDownloader _videoDownloader;

        public SignatureCipherDecoder(IVideoDownloader videoDownloader)
        {
            _videoDownloader = videoDownloader;
        }

        public async Task<string> Decode(string content, string s)
        {
            Match match = Regex.Match(
                content,
                @"\bsrc=\""(/s/player/[^""]+base.js)\""",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(200));
            if (match.Success)
            {
                string javaScript = await _videoDownloader.DownloadWebContent("https://www.youtube.com" + match.Groups[1].Value);
                Match functionName = Regex.Match(
                    javaScript,
                    @"(\w+)\s*=\s*function\(a\)\s*\{\s*a\s*=\s*a\.split\(\""\""\)",
                    RegexOptions.IgnoreCase,
                    TimeSpan.FromSeconds(10));

                if (functionName.Success)
                {
                    javaScript = Regex.Replace(javaScript, @";\s*\}\s*\)\s*\(\s*_yt_player\s*\)\s*;", $"; g.p_hold={functionName.Groups[1].Value}; }})(_yt_player);", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));
                    //File.WriteAllText("ytbase.js", javaScript);
                    Engine engine = new Engine()
                    .Execute("class XMLHttpRequest {};\n")
                    .Execute("var navigator = { };\n")
                    .Execute("var document = { location: { hostname: '' } };\n")
                    .Execute("var window = { location: { hostname: '' } };\n")
                    .Execute("var location = { hostname: '' };\n")
                    .Execute(javaScript)
                    .Execute($";var sOut = _yt_player.p_hold('{s}');");
                    JsValue sValue = engine.GetValue("sOut");
                    s = sValue.AsString();
                }
            }
            return s;
        }
    }
}
