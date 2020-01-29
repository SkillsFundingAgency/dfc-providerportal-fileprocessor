using Dfc.CourseDirectory.Common.Interfaces;
using Dfc.CourseDirectory.Models.Interfaces.Apprenticeships;
using Dfc.CourseDirectory.Models.Interfaces.Providers;
using Dfc.CourseDirectory.Models.Models.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Dfc.ProviderPortal.FileProcessor.Provider
{
    public interface IApprenticeshipBulkUploadService
    {
        int CountCsvLines(Stream stream);
        List<string> ValidateAndUploadCSV(ILogger log,Stream stream,string fileName);

        Task ProcessApprenticeshipFileAsync(ILogger log, CloudStorageAccount cloudStorageAccount, string containerName, string fileName, Stream fileStream);
       
        Task CreateErrorFileAsync(ILogger log, string fileName, Stream stream, CloudStorageAccount cloudStorageAccount, string containerName, string error);

        Task<bool> SetBulkUploadStatus(ILogger log, IProvider provider, Stream stream, int rowCount = 0);
        Task<bool> ClearBulkUploadStatus(ILogger log, IProvider provider, Stream stream);


    }
}
