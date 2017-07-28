using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;

namespace WordlyFBMessenger
{
    public static class GenericWebHookCSharp
    {
        [FunctionName("GenericWebHookCsharp")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            //string challenge = req.GetQueryNameValuePairs().Where(x => x.Key == "hub.challenge").First().Value;

            dynamic pullRequestObj = await req.Content.ReadAsAsync<ExpandoObject>();
            string word = null;
            string recipientid = null;
            string response = "";
            try
            {
                var entires = pullRequestObj.entry[0];
                var messaging = entires.messaging;

                var message = messaging[0];
                recipientid = message.sender.id;

                var msg = message.message;
                string prJsonString = JsonConvert.SerializeObject(pullRequestObj);
                log.Info(prJsonString);

                word = msg.text;

                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                word = rgx.Replace(word, "");
            }
            catch (RuntimeBinderException e)
            {
            }

            if (!string.IsNullOrWhiteSpace(word))
            {
                var definitions = await MultiLevelLookUp(word, LookupCachedWord, LookupMarimWebsterStudent2, LookupMarimWebsterStudent3, log);
                definitions = definitions.Select(x => ":" + x);
                response = string.Join("\r\n\r\n", definitions);
            }

            await SendResponse(recipientid, response);

            return req.CreateResponse(HttpStatusCode.OK, $"reply sent");
        }

        public static string ParseFBMessage(dynamic messageRequestContent, TraceWriter log = null)
        {
            // Get request body
            var entires = messageRequestContent.entry[0];
            var messaging = entires.messaging;

            var message = messaging[0];
            var recipientid = message.sender.id;

            var msg = message.message;
            string prJsonString = JsonConvert.SerializeObject(messageRequestContent);

            string word = msg.text;
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            word = rgx.Replace(word, "");
            return word;
        }

        public async static Task<IEnumerable<string>> MultiLevelLookUp(string word, Func<string, Task<string>> lookupFunc1, Func<string, Task<string>> lookupFunc2, Func<string, Task<string>> lookupFunc3, TraceWriter log = null)
        {
            var xmlText = "";
            var definitions = new List<string>();
            if (lookupFunc1 != null)
            {
                log?.Info($"1st level lookup: {word}");
                xmlText = await lookupFunc1(word);
                definitions = ParseMariamWebsterWordDefinition(xmlText).ToList();
                if (definitions.Any())
                {
                    return definitions;
                }
            }

            if (lookupFunc2 != null)
            {
                log?.Info($"2nd level lookup: {word}");
                xmlText = await lookupFunc2(word);
                definitions = ParseMariamWebsterWordDefinition(xmlText).ToList();
                if (definitions.Any())
                {
                    SaveWordDefinition(word, xmlText);
                    return definitions;
                }
            }

            if (lookupFunc3 != null)
            {
                log?.Info($"3nd level lookup: {word}");
                xmlText = await lookupFunc3(word);
                definitions = ParseMariamWebsterWordDefinition(xmlText).ToList();
                if (definitions.Any())
                {
                    SaveWordDefinition(word, xmlText);
                    return definitions;
                }
            }

            return definitions;
        }

        public static async Task SendResponse(string recipientId, string messageText)
        {
            if (string.IsNullOrWhiteSpace(recipientId))
                return;

            if (string.IsNullOrWhiteSpace(messageText))
                messageText = "?";

            if (messageText.Length > 630)
            {
                messageText = messageText.Substring(0, 630);
                messageText += " ...";
            }
            var messageData = new
            {
                recipient = new { id = recipientId },
                message = new
                {
                    text = messageText
                }
            };
            var stringPayload = JsonConvert.SerializeObject(messageData);

            var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");

            // httpContent.Headers.Add("access_token", "EAAbWQxB5y9YBAFhiw217XHM5QHCVIq9rx4bAnHHKU899D8cmIC7JzFbHawZBcF4B2wfxcHEWXCPFdKndGRT5RZCK1RmWjJuFZBZBIlCtZCI5VKE7Y9dxZBrDM2t78zWJVSKZCxzDGRwWIo23M9Apv4avZCCWlHKHniUlRL1OKKLD4gZDZD");

            using (var client = new HttpClient())
            {
                var httpResponseMessage = await client.PostAsync("https://graph.facebook.com/v2.6/me/messages?access_token=EAAbWQxB5y9YBAFhiw217XHM5QHCVIq9rx4bAnHHKU899D8cmIC7JzFbHawZBcF4B2wfxcHEWXCPFdKndGRT5RZCK1RmWjJuFZBZBIlCtZCI5VKE7Y9dxZBrDM2t78zWJVSKZCxzDGRwWIo23M9Apv4avZCCWlHKHniUlRL1OKKLD4gZDZD", httpContent);
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

        public static IEnumerable<string> ParseMariamWebsterWordDefinition(string xmlText)
        {
            try
            {
                var xml = XDocument.Parse(xmlText);

                var definittions = xml.Root.Descendants("dt")
                    .Select(x =>
                    {
                        string definition = x.FirstNode is XText ? x.FirstNode.ToString().Replace(":", "").Trim() : null;
                        if (!string.IsNullOrWhiteSpace(definition))
                            return definition;

                        definition = x.Descendants("un").Select(y => y.FirstNode.ToString()).FirstOrDefault()?.Trim();
                        if (!string.IsNullOrWhiteSpace(definition))
                            return definition;

                        definition = x.Descendants("sx").Select(y => y.FirstNode.ToString()).FirstOrDefault()?.Trim();
                        if (!string.IsNullOrWhiteSpace(definition))
                            return definition;

                        return definition;
                    })
                    .ToList();
                return definittions;
            }
            catch (XmlException e)
            {
                return new List<string>();
            }
        }

        public static CloudTable GetCloudTableInstance()
        {
            if (_cloudTable == null)
            {
                string connStr = "DefaultEndpointsProtocol=https;AccountName=wordlystorage;AccountKey=fyQUadfUyDnFjSCGTbX8D7AF+vmzefI4oy6rWo7S+vhYoQILC+H5gtC7I7MUQ99Owm/VOYe2XAym8qba0MSIYw==;EndpointSuffix=core.windows.net";
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=wordlystorage;AccountKey=fyQUadfUyDnFjSCGTbX8D7AF+vmzefI4oy6rWo7S+vhYoQILC+H5gtC7I7MUQ99Owm/VOYe2XAym8qba0MSIYw==;EndpointSuffix=core.windows.net");
                var tableclient = storageAccount.CreateCloudTableClient();
                _cloudTable = tableclient.GetTableReference("MWStudentWordDefinitions");
                return _cloudTable;
            }
            return _cloudTable;
        }

        public static Task SaveWordDefinition(string word, string xmlDefinition)
        {
            var table = GetCloudTableInstance();
            var tableOperation = TableOperation.InsertOrReplace(new WordDefinitionCacheEntry()
            {
                PartitionKey = "MWStudent",
                RowKey = word,
                Timestamp = DateTime.UtcNow,
                XmlDefinition = xmlDefinition
            });
            return table.ExecuteAsync(tableOperation);
        }

        private static CloudTable _cloudTable;

        public static async Task<string> LookupCachedWord(string word)
        {
            var table = GetCloudTableInstance();
            var readOperation = TableOperation.Retrieve<WordDefinitionCacheEntry>(partitionKey: "MWStudent", rowkey: word);
            var result = await table.ExecuteAsync(readOperation);
            var definition = result.Result as WordDefinitionCacheEntry;
            if (definition != null)
            {
                return definition.XmlDefinition;
            }
            return "";
        }
    }

    public class WordDefinitionCacheEntry : TableEntity
    {
        public string XmlDefinition { get; set; }
    }

    public class UserAccessLogEntry : TableEntity
    {
        public string UserId { get; set; }
        public string IPAddress { get; set; }
        public string WordLookup { get; set; }
    }
}