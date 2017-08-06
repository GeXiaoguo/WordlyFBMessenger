using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace WordlyFBMessenger
{
    public static class MariamWebseter
    {
        public static async Task<Stream> DownlaodAudioAsync(string fileName)
        {
            char partition = fileName.First();
            string audioUrl = $@"http://media.merriam-webster.com/soundc11/{partition}/{fileName}?key=804c8987-5ac7-46aa-a41c-09341db46050";
            using (var client = new HttpClient())
            {
                var responseMessage = await client.GetAsync(audioUrl);
                return await responseMessage.Content.ReadAsStreamAsync();
            }
        }

        public static async Task<string> LookupMarimWebsterStudent2(string word)
        {
            string student2Url = @"http://www.dictionaryapi.com/api/v1/references/sd2/xml/" + word + @"?key=804c8987-5ac7-46aa-a41c-09341db46050";
            using (var client = new HttpClient())
            {
                var responseMessage = await client.GetAsync(student2Url);
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }

        public static async Task<string> LookupMarimWebsterStudent3(string word)
        {
            string student3Url = @"http://www.dictionaryapi.com/api/v1/references/sd3/xml/" + word + @"?key=72059f3b-7a46-4634-8961-2c2a5680a0eb";
            using (var client = new HttpClient())
            {
                var responseMessage = await client.GetAsync(student3Url);
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }
        public static IEnumerable<string> ParseAudios(string xmlText)
        {
            try
            {
                var xml = XDocument.Parse(xmlText);

                var definittions = xml.Root.Descendants("wav")
                    .Select(x => x.FirstNode is XText ? x.FirstNode.ToString().Replace(":", "").Trim() : null)
                    .Distinct()
                    .ToList();
                return definittions;
            }
            catch (XmlException e)
            {
                return new List<string>();
            }
        }

        public static WordDefinitions Parse(string word, string xmlText)
        {
            var defs = ParseDefinitions(xmlText);
            var audios = ParseAudios(xmlText);
            return new WordDefinitions(){Word = word, Entreis = defs, AudioFiles = audios};
        }

        public static IEnumerable<DefinitionEntry> ParseDefinitions(string xmlText)
        {
            try
            {
                var xml = XDocument.Parse(xmlText);

                var definittions = xml.Root.Descendants("dt")
                    .Select(x =>
                    {
                        string vi = x.Descendants("vi")
                            .FirstOrDefault()?.Value
                            .Trim()
                            .Replace(@"\n", "");

                        vi = vi == null ? vi : Regex.Replace(vi, @"\s+", " ");

                        string definition = x.FirstNode is XText ? x.FirstNode.ToString().Replace(":", "").Trim() : null;
                        if (!string.IsNullOrWhiteSpace(definition))
                            return new DefinitionEntry() { Definition = definition, Usage = vi };

                        definition = x.Descendants("un").Select(y => y.FirstNode.ToString()).FirstOrDefault()?.Trim();
                        if (!string.IsNullOrWhiteSpace(definition))
                            return new DefinitionEntry() { Definition = definition, Usage = vi };

                        definition = x.Descendants("sx").Select(y => y.FirstNode.ToString()).FirstOrDefault()?.Trim();
                        if (!string.IsNullOrWhiteSpace(definition))
                            return new DefinitionEntry() { Definition = definition, Usage = vi };

                        return new DefinitionEntry() { Definition = definition, Usage = vi };
                    })
                    .ToList();
                return definittions;
            }
            catch (XmlException e)
            {
                return new List<DefinitionEntry>();
            }
        }
    }
}