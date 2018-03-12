using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using WordlyFBMessenger;

namespace WordlyFBMessengerTests
{
    [TestFixture]
    public class FaceBookMessengerTests
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
            var content = FaceBookMessenger.ParseTextMessage(messageDynamic);
            Assert.AreEqual(expectedWord, content.word);
        }
    }
}