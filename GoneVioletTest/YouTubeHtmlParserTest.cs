using GoneViolet;
using System.IO;
using System.Threading.Tasks;

namespace GoneVioletTest
{
    [TestClass]
    [DeploymentItem("Content", "Content")]
    public class YouTubeHtmlParserTest
    {
        [TestMethod]
        public async Task ParseContentVideoTest()
        {
            Assert.IsTrue(Directory.Exists("Content"));
            string[] items = Directory.GetFiles("Content");
            for (int i = 0; i < items.Length; i += 1)
            {
                YouTubeHtmlParser parser = new YouTubeHtmlParser(new SignatureCipherDecoder(null));
                string result = await parser.ParseVideo(items[i]);
            }
        }
    }
}