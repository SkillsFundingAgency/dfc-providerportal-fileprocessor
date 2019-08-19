using Dfc.CourseDirectory.Models.Models.Courses;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Dfc.ProviderPortal.FileProcessor.Provider
{
    public interface IProviderFileImporter
    {
        // The UoW.
        Task ProcessFileAsync(ILogger log, CloudStorageAccount cloudStorageAccount, string containerName, string fileName, Stream fileStream);

        // Independently-callable workers
        List<BulkUploadCourse> ParseCsvFile(ILogger log, string fileName, Stream stream, out List<string> errors);
        Task CreateErrorFileAsync(ILogger log, string fileName, Stream stream, CloudStorageAccount cloudStorageAccount, string containerName, string error);
    }
}
