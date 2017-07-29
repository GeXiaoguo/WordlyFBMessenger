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
        [FunctionName("GenericWebHookCsharp")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            //string challenge = req.GetQueryNameValuePairs().Where(x => x.Key == "hub.challenge").First().Value;

            var messageHttpContent = await req.Content.ReadAsAsync<ExpandoObject>();

            string response = "";

            (string word, string recipientId) = FaceBookMessenger.ParseTextMessage(messageHttpContent, log);

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
                definitions = definitions.Select(x => ":" + x);
                response = string.Join("\r\n\r\n", definitions);
            }

            await FaceBookMessenger.SendTextResponse(recipientId, response);

            return req.CreateResponse(HttpStatusCode.OK, $"reply sent");
        }

        public async static Task<IEnumerable<string>> CachedLookUp(string word, TraceWriter log = null)
        {
            var definitions = new List<string>();

            log?.Info($"cached word definition lookup: {word}");
            string xmlText = await AzureTableStorage.LookupCachedWord(word);

            definitions = MariamWebseter.ParseMariamWebsterWordDefinition(xmlText).ToList();
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