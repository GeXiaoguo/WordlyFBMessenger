using System;
using System.Collections.Generic;
using System.Dynamic;
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
    public class WordDefinitionCacheEntry : TableEntity
    {
        public string XmlDefinition { get; set; }
    }

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

            string response = "";

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
                var definitionLines = definitions.Select(x => ":" + x.Item1 + ( x.Item2==null? null : "\r\n-" + x.Item2));
                response = string.Join("\r\n\r\n", definitionLines);
            }

            await FaceBookMessenger.SendTextResponse(recipientId, response);

            return req.CreateResponse(HttpStatusCode.OK, $"reply sent");
        }

        public async static Task<IEnumerable<ValueTuple<string, string>>> CachedLookUp(string word, TraceWriter log = null)
        {
            log?.Info($"cached word definition lookup: {word}");
            string xmlText = await AzureTableStorage.LookupCachedWord(word);

            var definitions = MariamWebseter.ParseMariamWebsterWordDefinition(xmlText).ToList();
            if (definitions.Any())
            {
                return definitions;
            }

            log?.Info($"2nd level lookup: {word}");
            xmlText = await MariamWebseter.LookupMarimWebsterStudent2(word);
            definitions = MariamWebseter.ParseMariamWebsterWordDefinition(xmlText).ToList();
            if (definitions.Any())
            {
                AzureTableStorage.SaveWordDefinition(word, xmlText);
                return definitions;
            }

            log?.Info($"3nd level lookup: {word}");
            xmlText = await MariamWebseter.LookupMarimWebsterStudent3(word);
            definitions = MariamWebseter.ParseMariamWebsterWordDefinition(xmlText).ToList();
            if (definitions.Any())
            {
                AzureTableStorage.SaveWordDefinition(word, xmlText);
                return definitions;
            }

            return definitions;
        }
    }
}