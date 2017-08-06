using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;

namespace WordlyFBMessenger
{
    public class UserActivityLogEntry : TableEntity
    {
        public string UserId { get; set; }
        public string IPAddress { get; set; }
        public string WordLookup { get; set; }
    }

    public static class GenericWebHookCSharp
    {
        /// <summary>
        /// has to be less than 1000 messages per day
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool CheckQuota(string userId)
        {
            var entries = AzureTableStorage.QueryUserActivitySince(userId, DateTimeOffset.UtcNow - TimeSpan.FromDays(1));
            if (entries.Count() > 1000)
                return false;

            return true;
        }

        [FunctionName("GenericWebHookCsharp")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            //string challenge = req.GetQueryNameValuePairs().Where(x => x.Key == "hub.challenge").First().Value;
            var messageHttpContent = await req.Content.ReadAsAsync<ExpandoObject>();

            string jsonMessage = "";

            (string word, string recipientId) = FaceBookMessenger.ParseTextMessage(messageHttpContent);

            if (!CheckQuota(recipientId))
            {
                return req.CreateResponse((HttpStatusCode)429, "too many messages");
            }

            string ipAddress = "";
            if (req.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop;
                prop = (RemoteEndpointMessageProperty)req.Properties[RemoteEndpointMessageProperty.Name];
                ipAddress = prop.Address;
            }

            await AzureTableStorage.LogUserActivity(recipientId, word, ipAddress);

            if (!string.IsNullOrWhiteSpace(word))
            {
                var definitions = await CachedLookUp(word, log);
                jsonMessage = FaceBookMessenger.FormatMessage(recipientId, definitions);
            }

            await FaceBookMessenger.SendJsonMessage(jsonMessage);

            return req.CreateResponse(HttpStatusCode.OK, $"reply sent");
        }

        public static IEnumerable<string> DownloadAndUpdateAudios(IEnumerable<string> waveFiles)
        {
            var mp3files = new ConcurrentBag<string>();
            Parallel.ForEach(waveFiles, wavefile =>
            {
                var mp3 = DownloadAndUpdateAudio(wavefile).Result;
                mp3files.Add(mp3);
            });
            return mp3files;
        }

        public static async Task<string> DownloadAndUpdateAudio(string waveFile)
        {
            if (string.IsNullOrWhiteSpace(waveFile))
                return "";

            var stream = await MariamWebseter.DownlaodAudioAsync(waveFile);

            return AzureBlobStorage.Upload(waveFile, stream);
        }

        public async static Task<WordDefinitions> CachedLookUp(string word, TraceWriter log = null)
        {
            log?.Info($"cache lookup: {word}");
            string xmlText = await AzureTableStorage.LookupCachedWord(word);

            var definitions = MariamWebseter.Parse(word, xmlText);
            if (definitions.Entreis.Any())
            {
                return definitions;
            }

            log?.Info($"2nd level lookup: {word}");
            xmlText = await MariamWebseter.LookupMarimWebsterStudent2(word);
            definitions = MariamWebseter.Parse(word, xmlText);
            if (definitions.Entreis.Any())
            {
                definitions.AudioFiles = DownloadAndUpdateAudios(definitions.AudioFiles);
                await AzureTableStorage.SaveWordDefinition(word, xmlText, definitions.AudioFiles);
                return definitions;
            }

            log?.Info($"3nd level lookup: {word}");
            xmlText = await MariamWebseter.LookupMarimWebsterStudent3(word);
            definitions = MariamWebseter.Parse(word, xmlText);
            if (definitions.Entreis.Any())
            {
                definitions.AudioFiles = DownloadAndUpdateAudios(definitions.AudioFiles);
                await AzureTableStorage.SaveWordDefinition(word, xmlText, definitions.AudioFiles);
                return definitions;
            }

            return definitions;
        }
    }
}