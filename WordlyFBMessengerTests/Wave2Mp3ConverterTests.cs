using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using WordlyFBMessenger;

namespace WordlyFBMessengerTests
{
    [TestFixture]
    public class Wave2Mp3ConverterTests
    {
        [Test]
        public void ConverterTest()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;

            Directory.SetCurrentDirectory(testDir);

            string fullPath = Path.Combine(testDir, @"m-w\center01.wav");
            var waveBytes = File.ReadAllBytes(fullPath);
            string mp3FilePath = Path.Combine(@"m-w\result.mp3");
            var mp3stream = Wave2Mp3Converter.Convert(new MemoryStream(waveBytes));
            using (var fileStream = File.Create(mp3FilePath))
            {
                mp3stream.Seek(0, SeekOrigin.Begin);
                mp3stream.CopyTo(fileStream);
            }
        }
    }
}