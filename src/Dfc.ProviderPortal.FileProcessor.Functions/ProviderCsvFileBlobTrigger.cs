using System;
using System.IO;
using System.Threading.Tasks;
using Dfc.ProviderPortal.FileProcessor.Provider;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace Dfc.ProviderPortal.FileProcessor.Functions
{
    public class ProviderCsvFileBlobTrigger
    {
        private readonly IProviderFileImporter fileImporter;

        public ProviderCsvFileBlobTrigger(IProviderFileImporter fileImporter)
        {
            this.fileImporter = fileImporter;
        }

        [FunctionName("ProviderCsvFileBlobTrigger")]
        public async Task Run([BlobTrigger("%containerName%/{fileName}", Connection = "AzureWebJobsStorage")]Stream fileStream, string fileName, ILogger log)
        {
            // Hand-off all of the processing to a separate Unit of Work. 
            // This keeps the processing independent of the trigger so that it can be called by other means.
            CloudStorageAccount cloudStorageAccount = null;
            string containerName = null;
            try
            {
                cloudStorageAccount = GetCloudStorageAccount("AzureWebJobsStorage");
                containerName = Environment.GetEnvironmentVariable("containerName", EnvironmentVariableTarget.Process);
                await fileImporter.ProcessFileAsync(log, cloudStorageAccount, containerName, fileName, fileStream);
            }
            catch (Exception ex)
            {
                log.LogCritical(ex, $"Failed to process file {fileName}");
                await fileImporter.CreateErrorFileAsync(log, fileName, fileStream, cloudStorageAccount, containerName, $"{ex.Message} {ex.StackTrace}");
            }
        }

        private CloudStorageAccount GetCloudStorageAccount(string name)
        {
            var connectionString = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            return cloudStorageAccount;
        }
    }
}
