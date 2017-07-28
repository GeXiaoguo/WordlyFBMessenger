using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using WordlyFBMessenger;

namespace WordlyFBMessengerTests
{
    [TestFixture]
    public class GenericWebHookCSharpTests
    {
        [Test]
        public void ParsingTextMessage_Test()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            string fullPath = Path.Combine(testDir, @"FBMessage\TextMessage.json");

            var textMessage = JObject.Parse(File.ReadAllText(fullPath));
        }

        [Test]
        [TestCase(@"FBMessenger\ImageMessage.json", "")]
        [TestCase(@"FBMessenger\TextMessage.json", "hello")]
        public void ParsingImageMessage_Test(string messageFile, string expectedWord)
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            string fullPath = Path.Combine(testDir, messageFile);
            var jsonText = File.ReadAllText(fullPath);

            dynamic messageDynamic = JsonConvert.DeserializeObject(jsonText);

            string word = GenericWebHookCSharp.ParseFBMessage(messageDynamic);
            Assert.AreEqual(expectedWord, word);
        }

        [Test]
        [TestCase(@"m-w\goodStudent1.xml", 10)]
        [TestCase(@"m-w\CenterStudent1.xml", 8)]
        [TestCase(@"m-w\ComprehensionStudent1.xml", 1)]
        public void ParseXml(string wordFile, int expectedCount)
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            string fullPath = Path.Combine(testDir, wordFile);

            var xml = XDocument.Load(fullPath);
            var definitions = GenericWebHookCSharp.ParseMariamWebsterWordDefinition(xml.ToString());
            Assert.AreEqual(expectedCount, definitions.Count());
        }

        [Test]
        [TestCase("anchovy")]
        [TestCase("good")]
        public async Task LookupMarimWebster(string word)
        {
            TraceWriter log = null;
            var definitions = await GenericWebHookCSharp.MultiLevelLookUp(word, GenericWebHookCSharp.LookupCachedWord, GenericWebHookCSharp.LookupMarimWebsterStudent2, GenericWebHookCSharp.LookupMarimWebsterStudent3, log);
            Assert.IsTrue(definitions.Any());
        }

        [Test]
        public async Task SaveWordDefinition()
        {
            string wordFile = @"m-w\goodStudent1.xml";
            var testDir = TestContext.CurrentContext.TestDirectory;
            string fullPath = Path.Combine(testDir, wordFile);
            var xml = File.ReadAllText(fullPath);

            await GenericWebHookCSharp.SaveWordDefinition(word: "good", xmlDefinition: xml);

            var xmlResult = await GenericWebHookCSharp.LookupCachedWord(word: "good");
            Assert.AreEqual(xml, xmlResult);
        }
    }
}