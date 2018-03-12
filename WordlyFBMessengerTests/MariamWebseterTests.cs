using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure.WebJobs.Host;
using NUnit.Framework;
using WordlyFBMessenger;

namespace WordlyFBMessengerTests
{
    [TestFixture]
    public class MariamWebseterTests
    {
        [Test]
        [TestCase(@"m-w\goodStudent1.xml", 10)]
        [TestCase(@"m-w\CenterStudent1.xml", 8)]
        [TestCase(@"m-w\ComprehensionStudent1.xml", 1)]
        public void ParseXml(string wordFile, int expectedCount)
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            string fullPath = Path.Combine(testDir, wordFile);

            var xml = XDocument.Load(fullPath);
            var definitions = MariamWebseter.ParseDefinitions(xml.ToString());
            Assert.AreEqual(expectedCount, definitions.Count());
        }

        [Test]
        [TestCase("anchovy")]
        [TestCase("good")]
        public async Task LookupMarimWebster(string word)
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            Directory.SetCurrentDirectory(testDir);
            TraceWriter log = null;
            var definitions = await GenericWebHookCSharp.CachedLookUp(word, log);
            Assert.IsTrue(definitions.Entreis.Any());
        }

        [Test]
        [TestCase(@"m-w\goodStudent1.xml", 7)]
        [TestCase(@"m-w\CenterStudent1.xml", 1)]
        [TestCase(@"m-w\ComprehensionStudent1.xml", 1)]
        public void ParsePronounciation(string wordFile, int expectedCount)
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            string fullPath = Path.Combine(testDir, wordFile);

            var xml = XDocument.Load(fullPath);
            var waveFiles = MariamWebseter.ParseAudios(xml.ToString());
            Assert.AreEqual(expectedCount, waveFiles.Count());
        }

        [Test]
        [TestCase(@"m-w\goodStudent1.xml", 7)]
        [TestCase(@"m-w\CenterStudent1.xml", 1)]
        [TestCase(@"m-w\ComprehensionStudent1.xml", 1)]
        public void ParseAndDownload(string wordFile, int expectedCount)
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            string fullPath = Path.Combine(testDir, wordFile);

            Directory.SetCurrentDirectory(testDir);

            var xml = XDocument.Load(fullPath);
            var waveFiles = MariamWebseter.ParseAudios(xml.ToString());

            GenericWebHookCSharp.DownloadAndUpdateAudio(waveFiles.FirstOrDefault()).Wait();

            Assert.AreEqual(expectedCount, waveFiles.Count());
        }
    }
}