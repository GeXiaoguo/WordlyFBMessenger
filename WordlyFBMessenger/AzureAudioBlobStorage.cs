using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace WordlyFBMessenger
{
    public static class AzureBlobStorage
    {
        private static CloudBlobClient _cloudTableClient;
        private const string endping = "https://wordlystorage.blob.core.windows.net/m-w-pronunciation-audios";

        private static CloudBlobClient GetCloudBlobClientInstance()
        {
            if (_cloudTableClient == null)
            {
                string connStr = "DefaultEndpointsProtocol=https;AccountName=wordlystorage;AccountKey=fyQUadfUyDnFjSCGTbX8D7AF+vmzefI4oy6rWo7S+vhYoQILC+H5gtC7I7MUQ99Owm/VOYe2XAym8qba0MSIYw==;EndpointSuffix=core.windows.net";
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=wordlystorage;AccountKey=fyQUadfUyDnFjSCGTbX8D7AF+vmzefI4oy6rWo7S+vhYoQILC+H5gtC7I7MUQ99Owm/VOYe2XAym8qba0MSIYw==;EndpointSuffix=core.windows.net");
                _cloudTableClient = storageAccount.CreateCloudBlobClient();
            }
            return _cloudTableClient;
        }

        public static string Upload(string name, Stream inputStream)
        {
            //name = name.Replace(".wav", ".mp3");
            var client = GetCloudBlobClientInstance();
            var container = client.GetContainerReference("m-w-pronunciation-audios");
            var blob = container.GetBlockBlobReference(name);

            //using (var outStream = Wave2Mp3Converter.Convert(inputStream))
            {
                // inputStream.Seek(0, SeekOrigin.Begin);
                blob.Properties.ContentType = "audio/mpeg";
                blob.UploadFromStream(inputStream);
            }
            return name;
        }
    }
}