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
        public void FormatMessage_Test()
        {
            var definitions = new WordDefinitions()
            {
                Word = "center",
                Entreis = new[]
                {
                    new DefinitionEntry()
                    {
                        Definition = "a person or thing characterized by a particular concentration or activity",
                        Usage = "She likes to be the <it>center</it> of attention"
                    },
                    new DefinitionEntry()
                    {
                        Definition = "the middle point of a circle or a sphere equally distant from every point on the circumference or surfac",
                        Usage = "  "
                    },
                },
                AudioFiles = new[]
                {
                    "center01.wav",
                    "center02.wav"
                }
            };

            string jsonString = FaceBookMessenger.FormatMessage("1292686234187122", definitions);
        }

        [Test]
        public async Task SendAudioMessage_Test()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            string fullPath = Path.Combine(testDir, @"FBMessenger\StructuredMessageAudio.json");

            string textMessage = File.ReadAllText(fullPath);

            await FaceBookMessenger.SendStructuredMessage(textMessage);
        }

        [Test]
        public async Task SendStructuredMessage_Test()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            string fullPath = Path.Combine(testDir, @"FBMessenger\StructuredMessageSample.json");

            string textMessage = File.ReadAllText(fullPath);

            await FaceBookMessenger.SendStructuredMessage(textMessage);
        }

        [Test]
        public async Task SendStructuredMessage_Test1()
        {
            await FaceBookMessenger.SendStructuredMessage("1292686234187122", "hellooooo");
        }

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