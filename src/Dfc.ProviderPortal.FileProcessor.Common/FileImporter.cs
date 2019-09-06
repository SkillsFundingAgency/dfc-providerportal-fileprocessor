using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dfc.ProviderPortal.FileProcessor.Common
{
    public abstract class FileImporter
    {
        protected async Task<CopyState> CopyBlob(CloudStorageAccount cloudStorageAccount, string sourceContainerName, string sourceFileName, string destinationContainerName, string destinationFileName)
        {
            var blobStorageClient = cloudStorageAccount.CreateCloudBlobClient();

            var sourceContainer = blobStorageClient.GetContainerReference(sourceContainerName);
            var destinationContainer = blobStorageClient.GetContainerReference(destinationContainerName);

            var sourceBlob = sourceContainer.GetBlobReference(sourceFileName);

            var destinationBlob = destinationContainer.GetBlobReference(destinationFileName);

            var result = await destinationBlob.StartCopyAsync(sourceBlob.Uri);

            var copyResult = destinationBlob.CopyState;

            return copyResult;
        }

        protected async Task DeleteBlob(CloudStorageAccount cloudStorageAccount, string containerName, string fileName)
        {
            var blobStorageClient = cloudStorageAccount.CreateCloudBlobClient();
            var container = blobStorageClient.GetContainerReference(containerName);
            var blob = container.GetBlobReference(fileName);
            await blob.DeleteIfExistsAsync();
        }

        protected async Task CreateTextLinesBlob(CloudStorageAccount cloudStorageAccount, string containerName, string fileName, IEnumerable<string> lines)
        {
            var blobStorageClient = cloudStorageAccount.CreateCloudBlobClient();
            var container = blobStorageClient.GetContainerReference(containerName);
            var appendBlob = container.GetAppendBlobReference(fileName);
            if (!await appendBlob.ExistsAsync())
            {
                await appendBlob.CreateOrReplaceAsync();
            }

            foreach(string line in lines)
            {
                await appendBlob.AppendTextAsync(line);
                await appendBlob.AppendTextAsync(System.Environment.NewLine);
            }
        }
    }
}
