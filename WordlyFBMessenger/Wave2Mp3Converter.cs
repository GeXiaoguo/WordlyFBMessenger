using System.IO;
using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace WordlyFBMessenger
{
    public static class Wave2Mp3Converter
    {
        public static Stream Convert(Stream inputStream)
        {
            using (var fileReader = new WaveFileReader(inputStream))
            {
                Pcm8BitToSampleProvider sampleProvider = new Pcm8BitToSampleProvider(fileReader);
                SampleToWaveProvider waveProvider = new SampleToWaveProvider(sampleProvider);
                var outStream = new MemoryStream();
                using (var fileWriter = new LameMP3FileWriter(outStream, waveProvider.WaveFormat, 24))
                {
                    int buff_length = 1024 * 1024;
                    var buff = new byte[buff_length];
                    int position = 0;
                    int length = waveProvider.Read(buff, position, buff_length);

                    while (length > 0)
                    {
                        position += length;

                        fileWriter.Write(buff, (int)fileWriter.Position, length);
                        length = waveProvider.Read(buff, position, buff_length);
                    }
                    fileWriter.Flush();

                    return outStream;
                }
            }
        }
    }
}