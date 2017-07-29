using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace WordlyFBMessenger
{
    public static class FaceBookMessenger
    {
        public static (string word, string recipientId) ParseTextMessage(dynamic messageRequestContent, TraceWriter log = null)
        {
            try
            {
                // Get request body
                var entires = messageRequestContent.entry[0];
                var messaging = entires.messaging;

                var message = messaging[0];
                string recipientId = message.sender.id;

                var msg = message.message;
                string prJsonString = JsonConvert.SerializeObject(messageRequestContent);
                log?.Info(prJsonString);

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

            var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");

            // httpContent.Headers.Add("access_token", "EAAbWQxB5y9YBAFhiw217XHM5QHCVIq9rx4bAnHHKU899D8cmIC7JzFbHawZBcF4B2wfxcHEWXCPFdKndGRT5RZCK1RmWjJuFZBZBIlCtZCI5VKE7Y9dxZBrDM2t78zWJVSKZCxzDGRwWIo23M9Apv4avZCCWlHKHniUlRL1OKKLD4gZDZD");

            using (var client = new HttpClient())
            {
                var httpResponseMessage = await client.PostAsync("https://graph.facebook.com/v2.6/me/messages?access_token=EAAbWQxB5y9YBAFhiw217XHM5QHCVIq9rx4bAnHHKU899D8cmIC7JzFbHawZBcF4B2wfxcHEWXCPFdKndGRT5RZCK1RmWjJuFZBZBIlCtZCI5VKE7Y9dxZBrDM2t78zWJVSKZCxzDGRwWIo23M9Apv4avZCCWlHKHniUlRL1OKKLD4gZDZD", httpContent);
            }
        }
    }
}