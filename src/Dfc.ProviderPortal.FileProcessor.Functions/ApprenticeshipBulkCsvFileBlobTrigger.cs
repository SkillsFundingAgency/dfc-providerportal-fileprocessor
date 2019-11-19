using Dfc.ProviderPortal.FileProcessor.Provider;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Models.Models.Auth;

namespace Dfc.ProviderPortal.FileProcessor.Functions
{
    public class ApprenticeshipBulkCsvFileBlobTrigger
    {
        private readonly IApprenticeshipBulkUploadService fileImporter;

        public ApprenticeshipBulkCsvFileBlobTrigger(IApprenticeshipBulkUploadService fileImporter)
        {
            this.fileImporter = fileImporter;
        }

        [FunctionName("ApprenticeshipBulkCsvFileBlobTrigger")]
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
                fileImporter.ValidateAndUploadCSV(log,fileStream, fileName);
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
