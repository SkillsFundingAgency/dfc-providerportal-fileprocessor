﻿using Dfc.CourseDirectory.Models.Models.Courses;
using Dfc.CourseDirectory.Models.Interfaces.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Common.Interfaces;

namespace Dfc.ProviderPortal.FileProcessor.Provider
{
    public interface IProviderFileImporter
    {
        // The UoW.
        Task ProcessFileAsync(ILogger log, CloudStorageAccount cloudStorageAccount, string containerName, string fileName, Stream fileStream);

        // Independently-callable workers
        List<BulkUploadCourse> ParseCsvFile(ILogger log, string fileName, Stream stream, int ukPRN, out List<string> errors);
        Task CreateErrorFileAsync(ILogger log, string fileName, Stream stream, CloudStorageAccount cloudStorageAccount, string containerName, string error);

        Task<bool> SetBulkUploadStatus(ILogger log, IProvider provider, int rowCount = 0);
        Task<bool> ClearBulkUploadStatus(ILogger log, IProvider provider);
        Task<IResult> DeleteBulkUploadCourses(ILogger log, int ukPRN);
        Task<IResult> ArchiveCourses(ILogger log, int ukPRN);
    }
}
