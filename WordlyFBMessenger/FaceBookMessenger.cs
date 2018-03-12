using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace WordlyFBMessenger
{
    public static class FaceBookMessenger
    {
        public static string FormatMessage(string recipientId, WordDefinitions definitions)
        {
            if (!definitions.Entreis.Any())
                return"";

            var elememts = definitions.Entreis.Take(4).Select(entry => { return new { title = entry.Definition, subtitle = entry.Usage ?? " " }; });

            dynamic buttons = null;
            if (definitions.AudioFiles.Any())
            {
                buttons = new[]
                {
                    new
                    {
                        title = "Pronunciations",
                        type = "postback",
                        payload = definitions.Word
                    }
                };
            }

            var payload = new
            {
                template_type = "list",
                top_element_style = "compact",
                elements = elememts,
                buttons
            };

            var attachment = new
            {
                type = "template",
                payload
            };

            var recipient = new { id = recipientId };

            var jobject = new
            {
                recipient,
                message = new { attachment }
            };
            var jsonString = JsonConvert.SerializeObject(jobject);
            return jsonString;
        }

        public static (string word, string recipientId) ParseTextMessage(dynamic messageRequestContent)
        {
            try
            {
                // Get request body
                var entires = messageRequestContent.entry[0];
                var messaging = entires.messaging;

                var message = messaging[0];
                string recipientId = message.sender.id;

                var msg = message.message;

                string word = msg.text;

                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                word = rgx.Replace(word, "");

                return ( word, recipientId);
            }
            catch (RuntimeBinderException e)
            {
            }
            return ( "", "");
        }

        public static async Task SendTextResponse(string recipientId, string messageText)
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

            SendJsonMessage(stringPayload);
        }

        public static async Task SendJsonMessage(string jsonMessage)
        {
            var httpContent = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            // httpContent.Headers.Add("access_token", "EAAbWQxB5y9YBAFhiw217XHM5QHCVIq9rx4bAnHHKU899D8cmIC7JzFbHawZBcF4B2wfxcHEWXCPFdKndGRT5RZCK1RmWjJuFZBZBIlCtZCI5VKE7Y9dxZBrDM2t78zWJVSKZCxzDGRwWIo23M9Apv4avZCCWlHKHniUlRL1OKKLD4gZDZD");

            using (var client = new HttpClient())
            {
                var httpResponseMessage = await client.PostAsync("https://graph.facebook.com/v2.6/me/messages?access_token=EAAbWQxB5y9YBAFhiw217XHM5QHCVIq9rx4bAnHHKU899D8cmIC7JzFbHawZBcF4B2wfxcHEWXCPFdKndGRT5RZCK1RmWjJuFZBZBIlCtZCI5VKE7Y9dxZBrDM2t78zWJVSKZCxzDGRwWIo23M9Apv4avZCCWlHKHniUlRL1OKKLD4gZDZD", httpContent);
            }
        }

        public static async Task SendStructuredMessage(string recipientId, string messageText)
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
                    attachment = new
                    {
                        type = "template",
                        payload = new
                        {
                            template_type = "button",
                            text = messageText,
                            buttons = new[]
                            {
                                //new
                                //{
                                //    type = "web_url",
                                //    url = "https://petersapparel.parseapp.com",
                                //    title = "Show This Website"
                                //}

                                new
                                {
                                    type = "postback",
                                    title = "lookup another word: weather",
                                    payload = "weather"
                                }
                            }
                        }
                    }
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

        public static async Task SendStructuredMessage(string structuredJsonMessage)
        {
            var httpContent = new StringContent(structuredJsonMessage, Encoding.UTF8, "application/json");

            // httpContent.Headers.Add("access_token", "EAAbWQxB5y9YBAFhiw217XHM5QHCVIq9rx4bAnHHKU899D8cmIC7JzFbHawZBcF4B2wfxcHEWXCPFdKndGRT5RZCK1RmWjJuFZBZBIlCtZCI5VKE7Y9dxZBrDM2t78zWJVSKZCxzDGRwWIo23M9Apv4avZCCWlHKHniUlRL1OKKLD4gZDZD");

            using (var client = new HttpClient())
            {
                var httpResponseMessage = await client.PostAsync("https://graph.facebook.com/v2.6/me/messages?access_token=EAAbWQxB5y9YBAFhiw217XHM5QHCVIq9rx4bAnHHKU899D8cmIC7JzFbHawZBcF4B2wfxcHEWXCPFdKndGRT5RZCK1RmWjJuFZBZBIlCtZCI5VKE7Y9dxZBrDM2t78zWJVSKZCxzDGRwWIo23M9Apv4avZCCWlHKHniUlRL1OKKLD4gZDZD", httpContent);
            }
        }
    }
}