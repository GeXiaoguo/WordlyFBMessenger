using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace WordlyFBMessenger
{
    public static class AzureTableStorage
    {
        private static CloudTableClient _cloudTableClient;

        public static async Task<string> LookupCachedWord(string word)
        {
            var table = GetCloudTableClientInstance().GetTableReference("MWStudentWordDefinitios");
            var readOperation = TableOperation.Retrieve<WordDefinitionCacheEntry>(partitionKey: "MWStudent", rowkey: word);
            var result = await table.ExecuteAsync(readOperation);
            var definition = result.Result as WordDefinitionCacheEntry;
            if (definition != null)
            {
                return definition.XmlDefinition;
            }
            return "";
        }

        public static Task LogUserActivity(string userId, string word, string ip)
        {
            var table = GetCloudTableClientInstance().GetTableReference("UserActivity");
            var tableOperation = TableOperation.InsertOrReplace(new UserActivityLogEntry()
            {
                PartitionKey = userId,
                RowKey = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                WordLookup = word,
                IPAddress = ip
            });
            return table.ExecuteAsync(tableOperation);
        }

        private static CloudTableClient GetCloudTableClientInstance()
        {
            if (_cloudTableClient == null)
            {
                string connStr = "DefaultEndpointsProtocol=https;AccountName=wordlystorage;AccountKey=fyQUadfUyDnFjSCGTbX8D7AF+vmzefI4oy6rWo7S+vhYoQILC+H5gtC7I7MUQ99Owm/VOYe2XAym8qba0MSIYw==;EndpointSuffix=core.windows.net";
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=wordlystorage;AccountKey=fyQUadfUyDnFjSCGTbX8D7AF+vmzefI4oy6rWo7S+vhYoQILC+H5gtC7I7MUQ99Owm/VOYe2XAym8qba0MSIYw==;EndpointSuffix=core.windows.net");
                _cloudTableClient = storageAccount.CreateCloudTableClient();
            }
            return _cloudTableClient;
        }

        public static Task SaveWordDefinition(string word, string xmlDefinition)
        {
            var table = GetCloudTableClientInstance().GetTableReference("MWStudentWordDefinitios");
            var tableOperation = TableOperation.InsertOrReplace(new WordDefinitionCacheEntry()
            {
                PartitionKey = "MWStudent",
                RowKey = word,
                Timestamp = DateTime.UtcNow,
                XmlDefinition = xmlDefinition
            });
            return table.ExecuteAsync(tableOperation);
        }
    }
}