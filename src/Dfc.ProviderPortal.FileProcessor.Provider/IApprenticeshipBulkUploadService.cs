using Dfc.CourseDirectory.Common.Interfaces;
using Dfc.CourseDirectory.Models.Interfaces.Apprenticeships;
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

       // Task ProcessFileAsync(ILogger log, CloudStorageAccount cloudStorageAccount, string containerName, string fileName, Stream fileStream);

        //// Independently-callable workers
        //List<BulkUploadCourse> ParseCsvFile(ILogger log, string fileName, Stream stream, int ukPRN, out List<string> errors);
        Task CreateErrorFileAsync(ILogger log, string fileName, Stream stream, CloudStorageAccount cloudStorageAccount, string containerName, string error);

        //Task<bool> SetBulkUploadStatus(ILogger log, IApprenticeship apprenticeship, int rowCount = 0);
        //Task<bool> ClearBulkUploadStatus(ILogger log, IApprenticeship apprenticeship);
        //Task<IResult> DeleteBulkUploadCourses(ILogger log, int ukPRN);
        //Task<IResult> ArchiveCourses(ILogger log, int ukPRN);
    }
}
