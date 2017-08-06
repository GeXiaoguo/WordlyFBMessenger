using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using WordlyFBMessenger;

namespace WordlyFBMessengerTests
{
    [TestFixture]
    public class AzureTableStorageTests
    {
        [Test]
        public async Task SaveWordDefinition_Test()
        {
            string wordFile = @"m-w\goodStudent1.xml";
            var testDir = TestContext.CurrentContext.TestDirectory;
            string fullPath = Path.Combine(testDir, wordFile);
            var xml = File.ReadAllText(fullPath);

            await AzureTableStorage.SaveWordDefinition(word: "good", xmlDefinition: xml, audioFiles: null);

            var xmlResult = await AzureTableStorage.LookupCachedWord(word: "good");
            Assert.AreEqual(xml, xmlResult);
        }

        [Test]
        public async Task QueryUserActivitySince_Test()
        {
            var entires = AzureTableStorage.QueryUserActivitySince("1292686234187122", DateTime.UtcNow - TimeSpan.FromMinutes(10)).ToList();
        }
    }
}