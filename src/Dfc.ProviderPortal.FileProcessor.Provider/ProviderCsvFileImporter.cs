using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Dfc.CourseDirectory.Common;
using Dfc.CourseDirectory.Common.Interfaces;
using Dfc.CourseDirectory.Models.Enums;
using Dfc.CourseDirectory.Models.Interfaces.Providers;
using Dfc.CourseDirectory.Models.Models.Courses;
using Dfc.CourseDirectory.Models.Models.Providers;
using Dfc.CourseDirectory.Models.Models.Regions;
using Dfc.CourseDirectory.Models.Models.Venues;
using Dfc.CourseDirectory.Services;
using Dfc.CourseDirectory.Services.Interfaces;
using Dfc.CourseDirectory.Services.Interfaces.CourseService;
using Dfc.CourseDirectory.Services.Interfaces.ProviderService;
using Dfc.CourseDirectory.Services.Interfaces.VenueService;
using Dfc.CourseDirectory.Services.ProviderService;
using Dfc.CourseDirectory.Services.VenueService;
using Dfc.ProviderPortal.FileProcessor.Common;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using static Dfc.CourseDirectory.Models.Helpers.Attributes.AlternativeName;

namespace Dfc.ProviderPortal.FileProcessor.Provider
{
    public class ProviderCsvFileImporter : FileImporter, IProviderFileImporter
    {
        private readonly ILarsSearchService _larsSearchService;
        private readonly ICourseService _courseService;
        private readonly IVenueService _venueService;
        private readonly IProviderService _providerService;
        private List<Venue> cachedVenues;

        public ProviderCsvFileImporter(ILarsSearchService larsSearchService, ICourseService courseService, IVenueService venueService, IProviderService providerService)
        {
            _larsSearchService = larsSearchService;
            _courseService = courseService;
            _venueService = venueService;
            _providerService = providerService;
        }

        // the UoW
        public async Task ProcessFileAsync(ILogger log, CloudStorageAccount cloudStorageAccount, string containerName, string fileName, Stream fileStream)
        {
            // 1. Check that this is a file we want to process.
            if(ToBeIgnored(log, fileName, fileStream))
            {
                // @ToDo: Tell someone.
                log.LogInformation($"Ignoring file [{fileName}].");
                return;
            }

            // 2. Parse the UK PRN from the filename
            int ukPRN = GetUKPRNFromFilename(fileName);
            if (0 == ukPRN)
            {
                log.LogError($"Failed to parse the Provider's UK PRN from the filename {fileName}. Cannot proceed.");
                return;
            }
            IProvider provider = FindProvider(ukPRN);
            if (null == provider)
            {
                log.LogError($"Failed to find provider with PRN {ukPRN}. Cannot proceed.");
                return;
            }


            // 3. Parse the courses from the file.
            List<string> errors;
            var bulkUploadCourses = ParseCsvFile(log, fileName, fileStream, ukPRN, out errors);
            if(null != errors && errors.Any())
            {
                // @ToDo: Tell someone.
                log.LogError($"File [{fileName}] failed validation.");
                await CreateErrorFileAsync(log, fileName, fileStream, cloudStorageAccount, containerName, errors);
                return;
            }
            if(null == bulkUploadCourses || bulkUploadCourses.Count == 0)
            {
                // @ToDo: Tell someone.
                log.LogError($"File [{fileName}] contained no courses.");
                await CreateErrorFileAsync(log, fileName, fileStream, cloudStorageAccount, containerName, errors);
                return;
            }

            // 4. Set the bulk upload status on the provider to "in progress"
            await SetBulkUploadStatus(log, provider, bulkUploadCourses.Count);

            // 5. Populate the courses with LARS data.
            bulkUploadCourses = PopulateLARSData(bulkUploadCourses, out errors);
            if (null != errors && errors.Any())
            {
                // @ToDo: Tell someone.
                log.LogError($"File [{fileName}] failed updated with LARS data.");
                await CreateErrorFileAsync(log, fileName, fileStream, cloudStorageAccount, containerName, errors);
            }

            // 6. Map bulkupload course objects to course objects
            string userId = "ProviderCsvFileImporter";
            var courses = MappingBulkUploadCourseToCourse(log, bulkUploadCourses, userId, ukPRN, out errors);
            if (null != errors && errors.Any())
            {
                // @ToDo: Tell someone. But keep going as the errors are attached to the course objects so we'll upload them.
                log.LogError($"File [{fileName}] failed updated with LARS data.");
                await CreateErrorFileAsync(log, fileName, fileStream, cloudStorageAccount, containerName, errors);
            }

            // 7. Delete existing courses for the provier.
            var result = await DeleteBulkUploadCourses(log, ukPRN);
            if(result.IsFailure)
            {
                log.LogError($"Failed to delete bulk upload courses for provider {ukPRN}.");
                errors.Add($"Failed to delete the existing bulk-uploaded courses.");
            }

            // 8. Import the new courses.
            await UploadCourses(log, courses);

            // 9. Mark as completed.
            await MarkAsProcessedAsync(log, fileName, fileStream, cloudStorageAccount, containerName);
            await ClearBulkUploadStatus(log, provider);
  
            log.LogInformation($"Successfully processed file [{fileName}]");
        }

        private IProvider FindProvider(int ukPRN)
        { 
            IProvider provider = null;
            try
            {
                var providerSearchResult = Task.Run(async () => await _providerService.GetProviderByPRNAsync(new ProviderSearchCriteria(ukPRN.ToString()))).Result;
                if (providerSearchResult.IsSuccess)
                {
                    provider = providerSearchResult.Value.Value.FirstOrDefault();
                }
            }
            catch (Exception)
            {
                // @ToDo: decide how to handle this
            }
            return provider;
        }


        public async Task<IResult> DeleteBulkUploadCourses(ILogger log, int ukPRN)
        {
            log.LogDebug($"Deleting bulk upload courses for Provider {ukPRN}");
            return await _courseService.DeleteBulkUploadCourses(ukPRN);
        }

        public async Task<IResult> ArchiveCourses(ILogger log, int ukPRN)
        {
            log.LogDebug($"Archiving courses for Provider {ukPRN}");
            var foo = _courseService.ChangeCourseRunStatusesForUKPRNSelection(ukPRN, (int)RecordStatus.BulkUploadPending, (int)RecordStatus.Archived).Result;
            var bar = _courseService.ChangeCourseRunStatusesForUKPRNSelection(ukPRN, (int)RecordStatus.BulkUploadReadyToGoLive, (int)RecordStatus.Archived).Result;
            return await _courseService.ChangeCourseRunStatusesForUKPRNSelection(ukPRN, (int)RecordStatus.Live, (int)RecordStatus.Archived);
        }

        private async Task UploadCourses(ILogger log, List<Course> courses)
        {
            int coursesCount = courses.Count;
            int currentCourse = 0;
            int successCount = 0;
            DateTime startTimeUtc = DateTime.UtcNow;
            log.LogInformation($"Commencing import of {coursesCount} courses at {startTimeUtc} UTC.");

            foreach (var course in courses)
            {
                log.LogInformation($"Adding course {++currentCourse} of {coursesCount}");

                var courseResult = await _courseService.AddCourseAsync(course);
                if (courseResult.IsSuccess && courseResult.HasValue)
                {
                    // Do nothing. Eventually we could have a count on successfully uploaded courses
                    successCount++;
                }
                else
                {
                    log.LogError($"The course is NOT BulkUploaded, LARS_QAN = { course.LearnAimRef }. Error -  { courseResult.Error }");
                }
            }
            DateTime finishTimeUtc = DateTime.UtcNow;

            log.LogInformation($"Course import started at {startTimeUtc} UTC completed at {finishTimeUtc} UTC. {successCount} of {coursesCount} courses imported.");
        }

        private bool ToBeIgnored(ILogger log, string fileName, Stream stream)
        {
            bool ignore = fileName.Contains("Archive") || fileName.EndsWith(".processed") || fileName.EndsWith(".error");
            return ignore;
        }

        private async Task MarkAsProcessedAsync(ILogger log, string fileName, Stream stream, CloudStorageAccount cloudStorageAccount, string containerName)
        {
            var destinationFileName = GenerateProcessedFilename(fileName);
            var copyResult = await CopyBlob(cloudStorageAccount, containerName, fileName, containerName, destinationFileName);
            if (Microsoft.WindowsAzure.Storage.Blob.CopyStatus.Success != copyResult.Status)
            {
                // @ToDo: Tell someone.
                return;
            }

            // Remove the source file. This makes the process look like a rename rather than a copy.
            await DeleteBlob(cloudStorageAccount, containerName, fileName);
        }

        private async Task CreateErrorFileAsync(ILogger log, string fileName, Stream stream, CloudStorageAccount cloudStorageAccount, string containerName, IEnumerable<string> errors)
        {
            var errorFileName = GenerateErrorFilename(fileName);
            await CreateTextLinesBlob(cloudStorageAccount, containerName, errorFileName, errors);
        }

        public async Task CreateErrorFileAsync(ILogger log, string fileName, Stream stream, CloudStorageAccount cloudStorageAccount, string containerName, string error)
        {
            List<string> errors = new List<string>() { error };
            await CreateErrorFileAsync(log, fileName, stream, cloudStorageAccount, containerName, errors);
        }

        private string GenerateProcessedFilename(string fileName)
        {
            string processedSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".processed";
            string processedFileName = $"{fileName}.{processedSuffix}";
            return processedFileName;
        }

        private string GenerateErrorFilename(string fileName)
        {
            string errorSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".error";
            string errorFileName = $"{fileName}.{errorSuffix}";
            return errorFileName;
        }

        private int GetUKPRNFromFilename(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return 0;

            // fileName is in the format:  "/<PRN>/Bulk Upload/Files/YYmmDD-HHmmss-<Provider Name>.csv"
            var path = fileName;
            var root = Path.GetPathRoot(path);
            while (true)
            {
                var temp = Path.GetDirectoryName(path);
                if (temp != null && temp.Equals(root))
                    break;
                path = temp;
            }

            // PRN is numeric...
            if(!int.TryParse(path, out int prn))
            {
                return 0;
            }

            return prn;
        }

        public List<BulkUploadCourse> ParseCsvFile(ILogger log, string fileName, Stream stream, int ukPRN, out List<string> errors)
        {
            // Lifted and shifted from BulkUploadService.
            var validationErrors = new List<string>();
            var bulkUploadcourses = new List<BulkUploadCourse>();
            string missingFieldsError = string.Empty;
            int missingFieldsErrorCount = 0;
            int bulkUploadLineNumber = 2;
            int tempCourseId = 0;
            string previousLearnAimRef = string.Empty;
            stream.Position = 0;

            try
            {



                cachedVenues = GetVenuesForProvider(ukPRN);

                using (var reader = new StreamReader(stream))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        csv.Read();
                        csv.ReadHeader();
                        while (csv.Read())
                        {
                            // To enable multiple missing fields error display
                            if (bulkUploadLineNumber.Equals(2))
                            {
                                string larsQan = string.Empty;
                                if (!csv.TryGetField("LARS_QAN", out larsQan))
                                {
                                    missingFieldsError += " 'LARS_QAN',"; missingFieldsErrorCount++;
                                }
                                string venue = string.Empty;
                                if (!csv.TryGetField("VENUE", out venue))
                                {
                                    missingFieldsError += " 'VENUE',"; missingFieldsErrorCount++;
                                }
                                string COURSE_NAME = string.Empty;
                                if (!csv.TryGetField("COURSE_NAME", out COURSE_NAME))
                                {
                                    missingFieldsError += " 'COURSE_NAME',"; missingFieldsErrorCount++;
                                }
                                string ID = string.Empty;
                                if (!csv.TryGetField("ID", out venue))
                                {
                                    missingFieldsError += " 'ID',"; missingFieldsErrorCount++;
                                }
                                string DELIVERY_MODE = string.Empty;
                                if (!csv.TryGetField("DELIVERY_MODE", out larsQan))
                                {
                                    missingFieldsError += " 'DELIVERY_MODE',"; missingFieldsErrorCount++;
                                }
                                string FLEXIBLE_START_DATE = string.Empty;
                                if (!csv.TryGetField("FLEXIBLE_START_DATE", out venue))
                                {
                                    missingFieldsError += " 'FLEXIBLE_START_DATE',"; missingFieldsErrorCount++;
                                }
                                string START_DATE = string.Empty;
                                if (!csv.TryGetField("START_DATE", out venue))
                                {
                                    missingFieldsError += " 'START_DATE',"; missingFieldsErrorCount++;
                                }
                                string URL = string.Empty;
                                if (!csv.TryGetField("URL", out larsQan))
                                {
                                    missingFieldsError += " 'URL',"; missingFieldsErrorCount++;
                                }
                                string COST = string.Empty;
                                if (!csv.TryGetField("COST", out venue))
                                {
                                    missingFieldsError += " 'COST',"; missingFieldsErrorCount++;
                                }
                                string COST_DESCRIPTION = string.Empty;
                                if (!csv.TryGetField("COST_DESCRIPTION", out larsQan))
                                {
                                    missingFieldsError += " 'COST_DESCRIPTION',"; missingFieldsErrorCount++;
                                }
                                string DURATION_UNIT = string.Empty;
                                if (!csv.TryGetField("DURATION_UNIT", out venue))
                                {
                                    missingFieldsError += " 'DURATION_UNIT',"; missingFieldsErrorCount++;
                                }
                                string DURATION = string.Empty;
                                if (!csv.TryGetField("DURATION", out larsQan))
                                {
                                    missingFieldsError += " 'DURATION',"; missingFieldsErrorCount++;
                                }
                                string STUDY_MODE = string.Empty;
                                if (!csv.TryGetField("STUDY_MODE", out STUDY_MODE))
                                {
                                    missingFieldsError += " 'STUDY_MODE',"; missingFieldsErrorCount++;
                                }
                                string ATTENDANCE_PATTERN = string.Empty;
                                if (!csv.TryGetField("ATTENDANCE_PATTERN", out venue))
                                {
                                    missingFieldsError += " 'ATTENDANCE_PATTERN',"; missingFieldsErrorCount++;
                                }
                                string WHO_IS_THIS_COURSE_FOR = string.Empty;
                                if (!csv.TryGetField("WHO_IS_THIS_COURSE_FOR", out larsQan))
                                {
                                    missingFieldsError += " 'WHO_IS_THIS_COURSE_FOR',"; missingFieldsErrorCount++;
                                }
                                string ENTRY_REQUIREMENTS = string.Empty;
                                if (!csv.TryGetField("ENTRY_REQUIREMENTS", out venue))
                                {
                                    missingFieldsError += " 'ENTRY_REQUIREMENTS',"; missingFieldsErrorCount++;
                                }
                                string WHAT_YOU_WILL_LEARN = string.Empty;
                                if (!csv.TryGetField("WHAT_YOU_WILL_LEARN", out COURSE_NAME))
                                {
                                    missingFieldsError += " 'WHAT_YOU_WILL_LEARN',"; missingFieldsErrorCount++;
                                }
                                string HOW_YOU_WILL_LEARN = string.Empty;
                                if (!csv.TryGetField("HOW_YOU_WILL_LEARN", out venue))
                                {
                                    missingFieldsError += " 'HOW_YOU_WILL_LEARN',"; missingFieldsErrorCount++;
                                }
                                string WHAT_YOU_WILL_NEED_TO_BRING = string.Empty;
                                if (!csv.TryGetField("WHAT_YOU_WILL_NEED_TO_BRING", out larsQan))
                                {
                                    missingFieldsError += " 'WHAT_YOU_WILL_NEED_TO_BRING',"; missingFieldsErrorCount++;
                                }
                                string HOW_YOU_WILL_BE_ASSESSED = string.Empty;
                                if (!csv.TryGetField("HOW_YOU_WILL_BE_ASSESSED", out venue))
                                {
                                    missingFieldsError += " 'HOW_YOU_WILL_BE_ASSESSED',"; missingFieldsErrorCount++;
                                }
                                string WHERE_NEXT = string.Empty;
                                if (!csv.TryGetField("WHERE_NEXT", out larsQan))
                                {
                                    missingFieldsError += " 'WHERE_NEXT',"; missingFieldsErrorCount++;
                                }
                                string ADULT_EDUCATION_BUDGET = string.Empty;
                                if (!csv.TryGetField("ADULT_EDUCATION_BUDGET", out venue))
                                {
                                    missingFieldsError += " 'ADULT_EDUCATION_BUDGET',"; missingFieldsErrorCount++;
                                }
                                string ADVANCED_LEARNER_OPTION = string.Empty;
                                if (!csv.TryGetField("ADVANCED_LEARNER_OPTION", out larsQan))
                                {
                                    missingFieldsError += " 'ADVANCED_LEARNER_OPTION',"; missingFieldsErrorCount++;
                                }
                                if (!csv.TryGetField("NATIONAL_DELIVERY", out larsQan))
                                {
                                    missingFieldsError += " 'NATIONAL_DELIVERY',"; missingFieldsErrorCount++;
                                }
                                if (!csv.TryGetField("REGION", out larsQan))
                                {
                                    missingFieldsError += " 'REGION',"; missingFieldsErrorCount++;
                                }
                                if (!csv.TryGetField("SUB_REGION", out larsQan))
                                {
                                    missingFieldsError += " 'SUB_REGION',"; missingFieldsErrorCount++;
                                }
                            }

                            if (string.IsNullOrEmpty(missingFieldsError))
                            {
                                bool isCourseHeader = false;
                                string currentLearnAimRef = csv.GetField("LARS_QAN").Trim();
                                string courseFor = csv.GetField("WHO_IS_THIS_COURSE_FOR").Trim();

                                if (bulkUploadLineNumber.Equals(2) || currentLearnAimRef != previousLearnAimRef || !string.IsNullOrEmpty(courseFor))
                                {
                                    isCourseHeader = true;
                                    tempCourseId++;
                                }

                                if (string.IsNullOrEmpty(currentLearnAimRef))
                                    validationErrors.Add($"Line { bulkUploadLineNumber }, LARS_QAN = { currentLearnAimRef } => LARS is missing.");

                                else
                                {
                                    var record = new BulkUploadCourse
                                    {
                                        IsCourseHeader = isCourseHeader,
                                        TempCourseId = tempCourseId,
                                        BulkUploadLineNumber = bulkUploadLineNumber,
                                        LearnAimRef = currentLearnAimRef,
                                        ProviderUKPRN = ukPRN,
                                        VenueName = csv.GetField("VENUE").Trim(),
                                        CourseName = csv.GetField("COURSE_NAME").Trim(),
                                        ProviderCourseID = csv.GetField("ID").Trim(),
                                        DeliveryMode = csv.GetField("DELIVERY_MODE").Trim(),
                                        FlexibleStartDate = csv.GetField("FLEXIBLE_START_DATE").Trim(),
                                        StartDate = csv.GetField("START_DATE").Trim(),
                                        CourseURL = csv.GetField("URL").Trim(),
                                        Cost = csv.GetField("COST").Trim(),
                                        CostDescription = csv.GetField("COST_DESCRIPTION").Trim(),
                                        DurationUnit = csv.GetField("DURATION_UNIT").Trim(),
                                        DurationValue = csv.GetField("DURATION").Trim(),
                                        StudyMode = csv.GetField("STUDY_MODE").Trim(),
                                        AttendancePattern = csv.GetField("ATTENDANCE_PATTERN").Trim(),
                                        National = csv.GetField("NATIONAL_DELIVERY").Trim(),
                                        Regions = csv.GetField("REGION").Trim(),
                                        SubRegions = csv.GetField("SUB_REGION").Trim()
                                    };

                                    if (isCourseHeader)
                                    {
                                        record.CourseDescription = courseFor; //csv.GetField("WHO_IS_THIS_COURSE_FOR").Trim();
                                        record.EntryRequirements = csv.GetField("ENTRY_REQUIREMENTS").Trim();
                                        record.WhatYoullLearn = csv.GetField("WHAT_YOU_WILL_LEARN").Trim();
                                        record.HowYoullLearn = csv.GetField("HOW_YOU_WILL_LEARN").Trim();
                                        record.WhatYoullNeed = csv.GetField("WHAT_YOU_WILL_NEED_TO_BRING").Trim();
                                        record.HowYoullBeAssessed = csv.GetField("HOW_YOU_WILL_BE_ASSESSED").Trim();
                                        record.WhereNext = csv.GetField("WHERE_NEXT").Trim();
                                        record.AdultEducationBudget = csv.GetField("ADULT_EDUCATION_BUDGET").Trim();
                                        record.AdvancedLearnerLoan = csv.GetField("ADVANCED_LEARNER_OPTION").Trim();
                                    }
                                    bulkUploadcourses.Add(record);
                                }
                                previousLearnAimRef = currentLearnAimRef;
                            }
                            bulkUploadLineNumber++;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(missingFieldsError))
                {
                    missingFieldsError = missingFieldsError.TrimEnd(',');
                    if (missingFieldsErrorCount.Equals(1))
                        validationErrors.Add($"Field with name { missingFieldsError } does not exist");
                    else
                        validationErrors.Add($"Fields with names { missingFieldsError } do not exist");
                    errors = validationErrors;
                    return null;
                }

                if (null == bulkUploadcourses || 0 == bulkUploadcourses.Count)
                {
                    validationErrors.Add("No course data in the uploaded file.");
                    errors = validationErrors;
                    return null;
                }

                errors = null;
                return bulkUploadcourses;
            }
            catch(Exception ex)
            {
                errors = validationErrors;
                log.LogCritical(ex,"ProviderCsvFileImporter failed.");
                return null;
            }
        }

        private List<LarsSearchResultItem> SearchLarsAsync(ILarsSearchCriteria criteria)
        {
            List<LarsSearchResultItem> results = new List<LarsSearchResultItem>();
            var result = Task.Run(async () => await _larsSearchService.SearchAsync(criteria)).Result;
            if(result.IsSuccess)
            {
                var ilsr = result.Value;
                if(null != ilsr)
                {
                    var enumerable = ilsr.Value;
                    if(null != enumerable)
                    {
                        results = enumerable.ToList();
                    }
                }
            }
            //).Result.Value.Value.ToList()

            return results;
        }

        public List<BulkUploadCourse> PopulateLARSData(List<BulkUploadCourse> bulkUploadcourses, out List<string> errors)
        {
            errors = new List<string>();
            List<int> totalErrorList = new List<int>();
            List<LarsSearchResultItem> cachedQuals = new List<LarsSearchResultItem>();

            foreach (var bulkUploadcourse in bulkUploadcourses.Where(c => c.IsCourseHeader == true).ToList())
            {
                ILarsSearchCriteria criteria = new LarsSearchCriteria(bulkUploadcourse.LearnAimRef, 10, 0, string.Empty, null);
                var qualifications = new List<LarsDataResultItem>();


                var cachedResult = cachedQuals != null ? cachedQuals.FirstOrDefault(o => o.LearnAimRef == criteria.Search) : null;

                List<LarsSearchResultItem> result = null;
                if (cachedResult == null)
                {

                    result = SearchLarsAsync(criteria);
                    var qual = result.FirstOrDefault();
                    if (qual != null)
                    {
                        cachedQuals.Add(qual);
                    }

                }
                else
                {
                    result = new List<LarsSearchResultItem> { cachedQuals.FirstOrDefault(o => o.LearnAimRef == criteria.Search) };
                }

                if (result.Count > 0)
                {

                    foreach (var item in result)
                    {
                        var larsDataResultItem = new LarsDataResultItem
                        {
                            LearnAimRef = item.LearnAimRef,
                            LearnAimRefTitle = item.LearnAimRefTitle,
                            NotionalNVQLevelv2 = item.NotionalNVQLevelv2,
                            AwardOrgCode = item.AwardOrgCode,
                            LearnAimRefTypeDesc = item.LearnAimRefTypeDesc,
                            CertificationEndDate = item.CertificationEndDate
                        };
                        qualifications.Add(larsDataResultItem);
                    }

                    if (qualifications.Count.Equals(0))
                    {
                        List<int> invalidLARSLineNumbers = bulkUploadcourses.Where(c => c.LearnAimRef == bulkUploadcourse.LearnAimRef).Select(l => l.BulkUploadLineNumber).ToList();

                        invalidLARSLineNumbers = CheckForErrorDuplicates(ref totalErrorList, invalidLARSLineNumbers);

                        if (invalidLARSLineNumbers.Count > 0)
                        {
                            errors.Add($"{ InvalidLARSLineNumbersToString(invalidLARSLineNumbers) }, " + $"LARS_QAN = { bulkUploadcourse.LearnAimRef } invalid LARS");
                        }

                    }
                    else if (qualifications.Count.Equals(1))
                    {
                        if (qualifications[0].CertificationEndDate != null && qualifications[0].CertificationEndDate < DateTime.Now)
                        {
                            List<int> invalidLARSLineNumbers = bulkUploadcourses.Where(c => c.LearnAimRef == bulkUploadcourse.LearnAimRef).Select(l => l.BulkUploadLineNumber).ToList();

                            invalidLARSLineNumbers = CheckForErrorDuplicates(ref totalErrorList, invalidLARSLineNumbers);

                            if (invalidLARSLineNumbers.Count > 0)
                            {
                                errors.Add($"{ InvalidLARSLineNumbersToString(invalidLARSLineNumbers) }, LARS_QAN = { bulkUploadcourse.LearnAimRef } expired LARS");
                            }

                        }
                        else
                        {
                            bulkUploadcourse.QualificationCourseTitle = qualifications[0].LearnAimRefTitle;
                            bulkUploadcourse.NotionalNVQLevelv2 = qualifications[0].NotionalNVQLevelv2;
                            bulkUploadcourse.AwardOrgCode = qualifications[0].AwardOrgCode;
                            bulkUploadcourse.QualificationType = qualifications[0].LearnAimRefTypeDesc;
                        }
                    }
                    else
                    {
                        string logMoreQualifications = string.Empty;
                        foreach (var qualification in qualifications)
                        {
                            logMoreQualifications += "( '" + qualification.LearnAimRefTitle + "' with Level " + qualification.NotionalNVQLevelv2 + " and AwardOrgCode " + qualification.AwardOrgCode + " ) ";
                        }
                        errors.Add($"We retrieve multiple qualifications ( { qualifications.Count.ToString() } ) for the LARS { bulkUploadcourse.LearnAimRef }, which are { logMoreQualifications } ");
                    }
                }
                else
                {
                    errors.Add($"Line {bulkUploadcourse.BulkUploadLineNumber}, LARS_QAN = { bulkUploadcourse.LearnAimRef }, invalid LARS");
                }

            }

            return bulkUploadcourses;
        }

        public List<int> CheckForErrorDuplicates(ref List<int> totalList, List<int> errorList)
        {

            for (int i = errorList.Count - 1; i >= 0; i--)
            {
                bool exists = totalList.Any(x => x.Equals(errorList[i]));
                if (exists)
                {
                    errorList.Remove(errorList[i]);
                }
                else
                {
                    totalList.Add(errorList[i]);
                }
            }
            return errorList;
        }

        public string InvalidLARSLineNumbersToString(List<int> invalidLARSLineNumbers)
        {
            string invalidLARSLineNumbersToString = string.Empty;

            if (invalidLARSLineNumbers.Count.Equals(1))
                invalidLARSLineNumbersToString = $"Line { invalidLARSLineNumbers[0] }";
            if (invalidLARSLineNumbers.Count > 1)
            {
                int lastNumber = invalidLARSLineNumbers[invalidLARSLineNumbers.Count - 1];
                invalidLARSLineNumbers.RemoveAt(invalidLARSLineNumbers.Count - 1);
                invalidLARSLineNumbersToString = "Lines ";
                invalidLARSLineNumbersToString += string.Join(", ", invalidLARSLineNumbers);
                invalidLARSLineNumbersToString += " and " + lastNumber;
            }

            return invalidLARSLineNumbersToString;
        }



        public List<Course> MappingBulkUploadCourseToCourse(ILogger log, List<BulkUploadCourse> bulkUploadCourses, string userId, int ukPRN, out List<string> errors)
        {
            errors = new List<string>();
            var validationMessages = new List<string>();

            var courses = new List<Course>();
            var listsCourseRuns = new List<BulkUploadCourseRun>();

            foreach (var bulkUploadCourse in bulkUploadCourses)
            {
                if (bulkUploadCourse.IsCourseHeader)
                {
                    var course = new Course();
                    course.id = Guid.NewGuid();
                    course.QualificationCourseTitle = bulkUploadCourse.QualificationCourseTitle;
                    course.LearnAimRef = bulkUploadCourse.LearnAimRef;
                    course.NotionalNVQLevelv2 = bulkUploadCourse.NotionalNVQLevelv2;
                    course.AwardOrgCode = bulkUploadCourse.AwardOrgCode;
                    course.QualificationType = bulkUploadCourse.QualificationType;
                    course.ProviderUKPRN = bulkUploadCourse.ProviderUKPRN;
                    course.CourseDescription = bulkUploadCourse.CourseDescription;
                    course.EntryRequirements = bulkUploadCourse.EntryRequirements;
                    course.WhatYoullLearn = bulkUploadCourse.WhatYoullLearn;
                    course.HowYoullLearn = bulkUploadCourse.HowYoullLearn;
                    course.WhatYoullNeed = bulkUploadCourse.WhatYoullNeed;
                    course.HowYoullBeAssessed = bulkUploadCourse.HowYoullBeAssessed;
                    course.WhereNext = bulkUploadCourse.WhereNext;
                    course.AdvancedLearnerLoan = bulkUploadCourse.AdvancedLearnerLoan.Equals("Yes", StringComparison.InvariantCultureIgnoreCase) ? true : false;
                    course.AdultEducationBudget = bulkUploadCourse.AdultEducationBudget.Equals("Yes", StringComparison.InvariantCultureIgnoreCase) ? true : false;
                    course.BulkUploadErrors = ParseBulkUploadErrors(bulkUploadCourse.BulkUploadLineNumber, _courseService.ValidateCourse(course));
                    course.IsValid = course.BulkUploadErrors.Any() ? false : true;

                    course.CreatedBy = userId;
                    course.CreatedDate = DateTime.Now;

                    course.UpdatedBy = bulkUploadCourse.TempCourseId.ToString();

                    courses.Add(course);
                }

                var courseRun = new CourseRun();
                courseRun.id = Guid.NewGuid();

                courseRun.DeliveryMode = GetValueFromDescription<DeliveryMode>(bulkUploadCourse.DeliveryMode);
                if (courseRun.DeliveryMode.Equals(DeliveryMode.Undefined))
                {
                    validationMessages.Add($"DeliveryMode is Undefined, because you have entered ( { bulkUploadCourse.DeliveryMode } ), Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                }

                // Call VenueService and for VenueName get VenueId (GUID) (Applicable only for type ClassroomBased)

                if (string.IsNullOrEmpty(bulkUploadCourse.VenueName))
                {
                    validationMessages.Add($"NO Venue Name for Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                }
                else
                {
                    //GetVenuesByPRNAndNameCriteria venueCriteria = new GetVenuesByPRNAndNameCriteria(bulkUploadCourse.ProviderUKPRN.ToString(), bulkUploadCourse.VenueName);
                    var venueResultCache = cachedVenues.Where(o => o.VenueName.ToLower() == bulkUploadCourse.VenueName.ToLower() && o.Status == VenueStatus.Live).ToList();

                    if (null != venueResultCache && venueResultCache.Count > 0)
                    {
                        //var venues = (IEnumerable<Venue>)venueResultCeche.Value.Value;
                        if (venueResultCache.Count().Equals(1))
                        {
                            if (venueResultCache.FirstOrDefault().Status.Equals(VenueStatus.Live))
                            {
                                courseRun.VenueId = new Guid(venueResultCache.FirstOrDefault().ID);
                            }
                            else
                            {
                                validationMessages.Add($"Venue is not LIVE (The status is { venueResultCache.FirstOrDefault().Status }) for VenueName { bulkUploadCourse.VenueName } - Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                            }
                        }
                        else
                        {
                            validationMessages.Add($"We have obtained muliple Venues for { bulkUploadCourse.VenueName } - Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                            if (venueResultCache.FirstOrDefault().Status.Equals(VenueStatus.Live))
                            {
                                courseRun.VenueId = new Guid(venueResultCache.FirstOrDefault().ID);
                            }
                            else
                            {
                                validationMessages.Add($"The selected Venue is not LIVE (The status is { venueResultCache.FirstOrDefault().Status }) for VenueName { bulkUploadCourse.VenueName } - Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                            }
                        }
                    }
                    else
                    {
                        validationMessages.Add($"We could NOT obtain a Venue for { bulkUploadCourse.VenueName } - Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                    }
                }


                courseRun.CourseName = bulkUploadCourse.CourseName;
                courseRun.ProviderCourseID = bulkUploadCourse.ProviderCourseID;

                courseRun.FlexibleStartDate = bulkUploadCourse.FlexibleStartDate.Equals("Yes", StringComparison.InvariantCultureIgnoreCase) ? true : false;

                DateTime specifiedStartDate;
                if (DateTime.TryParseExact(bulkUploadCourse.StartDate, "dd/MM/yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out specifiedStartDate))
                {
                    courseRun.StartDate = specifiedStartDate;
                }
                else if (DateTime.TryParse(bulkUploadCourse.StartDate, out specifiedStartDate))
                {
                    //Remove time
                    specifiedStartDate = specifiedStartDate.Date;
                    courseRun.StartDate = specifiedStartDate;
                }
                else
                {
                    courseRun.StartDate = null;
                    validationMessages.Add($"StartDate is NULL, because you have entered ( { bulkUploadCourse.StartDate } ), Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }. We are expecting the date in 'dd/MM/yyyy' format.");
                }

                courseRun.CourseURL = bulkUploadCourse.CourseURL;

                decimal specifiedCost;
                if (decimal.TryParse(bulkUploadCourse.Cost, out specifiedCost))
                {
                    courseRun.Cost = specifiedCost;
                }
                else
                {
                    courseRun.Cost = null;
                    validationMessages.Add($"Cost is NULL, because you have entered ( { bulkUploadCourse.Cost } ), Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                }

                courseRun.CostDescription = bulkUploadCourse.CostDescription;
                courseRun.DurationUnit = GetValueFromDescription<DurationUnit>(bulkUploadCourse.DurationUnit);
                if (courseRun.DurationUnit.Equals(DurationUnit.Undefined))
                {
                    validationMessages.Add($"DurationUnit is Undefined, because you have entered ( { bulkUploadCourse.DurationUnit } ), Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                }

                int specifiedDurationValue;
                if (int.TryParse(bulkUploadCourse.DurationValue, out specifiedDurationValue))
                {
                    courseRun.DurationValue = specifiedDurationValue;
                }
                else
                {
                    courseRun.DurationValue = null;
                    validationMessages.Add($"DurationValue is NULL, because you have entered ( { bulkUploadCourse.DurationValue } ), Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                }

                courseRun.StudyMode = GetValueFromDescription<StudyMode>(bulkUploadCourse.StudyMode);
                if (courseRun.StudyMode.Equals(StudyMode.Undefined))
                {
                    validationMessages.Add($"StudyMode is Undefined, because you have entered ( { bulkUploadCourse.StudyMode } ), Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                }
                courseRun.AttendancePattern = GetValueFromDescription<AttendancePattern>(bulkUploadCourse.AttendancePattern);
                if (courseRun.AttendancePattern.Equals(AttendancePattern.Undefined))
                {
                    validationMessages.Add($"AttendancePattern is Undefined, because you have entered ( { bulkUploadCourse.AttendancePattern } ), Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                }

                switch (bulkUploadCourse.National.ToUpperInvariant())
                {
                    case "YES":
                        {
                            courseRun.National = true;
                            var availableRegions = new SelectRegionModel();
                            courseRun.Regions = availableRegions.RegionItems.Select(x => x.Id).ToList();
                            break;
                        }
                    case "NO":
                        {
                            courseRun.National = false;
                            var regionResult = ParseRegionData(bulkUploadCourse.Regions, bulkUploadCourse.SubRegions);
                            if (regionResult.IsSuccess && regionResult.HasValue)
                            {
                                courseRun.Regions = regionResult.Value;
                            }
                            else if (regionResult.IsFailure)
                            {
                                validationMessages.Add($"Unable to get regions/subregions, Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                            }
                            break;
                        }
                    default:
                        {
                            courseRun.National = null;
                            validationMessages.Add($"Choose if you can deliver this course anywhere in England, Line { bulkUploadCourse.BulkUploadLineNumber },  LARS_QAN = { bulkUploadCourse.LearnAimRef }, ID = { bulkUploadCourse.ProviderCourseID }");
                            break;
                        }
                }


                courseRun.BulkUploadErrors = ParseBulkUploadErrors(bulkUploadCourse.BulkUploadLineNumber, _courseService.ValidateCourseRun(courseRun, ValidationMode.BulkUploadCourse));
                courseRun.RecordStatus = courseRun.BulkUploadErrors.Any() ? RecordStatus.BulkUploadPending : RecordStatus.BulkUploadReadyToGoLive;

                courseRun.CreatedBy = userId;
                courseRun.CreatedDate = DateTime.Now;

                listsCourseRuns.Add(new BulkUploadCourseRun { LearnAimRef = bulkUploadCourse.LearnAimRef, TempCourseId = bulkUploadCourse.TempCourseId, CourseRun = courseRun });
            }

            foreach (var course in courses)
            {
                int currentTempCourseId;
                if (int.TryParse(course.UpdatedBy, out currentTempCourseId))
                {
                    course.CourseRuns = listsCourseRuns.Where(cr => cr.LearnAimRef == course.LearnAimRef && cr.TempCourseId == currentTempCourseId).Select(cr => cr.CourseRun).ToList();
                }
                else
                {
                    validationMessages.Add($"Problem with parsing TempCourseId -  ( { course.UpdatedBy } ), LARS_QAN = { course.LearnAimRef }, CourseFor = { course.CourseDescription }");
                }

                course.UpdatedBy = null;
            }

            return courses;
        }

        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum)
                return default(T);

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;

                var alternativeName = Attribute.GetCustomAttribute(field, typeof(AlternativeNameAttribute)) as AlternativeNameAttribute;

                if (attribute != null)
                {
                    if (attribute.Description.Equals(description, StringComparison.InvariantCultureIgnoreCase))
                        return (T)field.GetValue(null);
                }
                if (alternativeName != null)
                {
                    if (alternativeName.Value.Equals(description, StringComparison.InvariantCultureIgnoreCase))
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name.Equals(description, StringComparison.InvariantCultureIgnoreCase))
                        return (T)field.GetValue(null);
                }
            }

            return default(T);
        }
        internal IResult<IEnumerable<string>> ParseRegionData(string regions, string subRegions)
        {
            List<string> totalList = new List<string>();

            var availableRegions = new SelectRegionModel();
            var availableSubRegions = availableRegions.RegionItems.SelectMany(x => x.SubRegion);
            var listOfRegions = regions.Split(";").Select(p => p.Trim()).ToList();
            var listOfSubregions = subRegions.Split(";").Select(p => p.Trim()).ToList();
            //Get regions
            foreach (var region in listOfRegions)
            {
                if (!string.IsNullOrWhiteSpace(region))
                {
                    var id = availableRegions.RegionItems.Where(x => x.RegionName.ToUpper() == region.ToUpper())
                                                     .Select(y => y.Id);
                    if (id.Count() > 0)
                    {
                        totalList.Add(id.FirstOrDefault());
                    }
                    else
                    {
                        return Result.Fail<IEnumerable<string>>("Problem with Bulk upload value");
                    }
                }
            }
            foreach (var subRegion in listOfSubregions)
            {
                if (!string.IsNullOrEmpty(subRegion))
                {
                    var id = availableSubRegions.Where(x => x.SubRegionName.ToUpper() == subRegion.ToUpper())
                                            .Select(y => y.Id);
                    if (id.Count() > 0)
                    {
                        totalList.Add(id.FirstOrDefault());
                    }
                    else
                    {
                        return Result.Fail<IEnumerable<string>>("Problem with Bulk upload value");
                    }
                }
            }
            return Result.Ok<IEnumerable<string>>(totalList);
        }
        internal IEnumerable<BulkUploadError> ParseBulkUploadErrors(int lineNumber, IList<KeyValuePair<string, string>> errors)
        {
            List<BulkUploadError> errorList = new List<BulkUploadError>();

            foreach (var error in errors)
            {
                //If non-bulk upload error
                if (error.Key == "NULL")
                {
                    continue;
                }
                BulkUploadError buError = new BulkUploadError
                {
                    LineNumber = lineNumber,
                    Header = error.Key,
                    Error = error.Value
                };
                errorList.Add(buError);
            }
            return errorList;
        }

        private List<Venue> GetVenuesForProvider(int ukPrn)
        {
            var venues = new List<Venue>();
            var searchCriteria = new VenueSearchCriteria(ukPrn.ToString(), string.Empty);

            var result = Task.Run(async () => await _venueService.SearchAsync(searchCriteria)).Result;
            if(result.IsSuccess)
            {
                var vsr = result.Value;
                if(null != vsr)
                {
                    var enumerable = vsr.Value;
                    if(null != enumerable)
                    {
                        venues = enumerable.ToList();
                    }
                }
            }
            else
            {
                throw new Exception($"Cannot process file. {result.Error}");
            }

            return venues;
        }

        public async Task<bool> ClearBulkUploadStatus(ILogger log, IProvider provider)
        {
            return await SetBulkUploadStatus(log, provider, 0);
        }

        public async Task<bool> SetBulkUploadStatus(ILogger log, IProvider provider, int rowCount = 0)
        {
            BulkUploadStatus bustatus = new BulkUploadStatus()
            {
                InProgress = (rowCount > 0),
                StartedTimestamp = (rowCount > 0) ? DateTime.Now : default(DateTime?),
                TotalRowCount = rowCount            
            };
            provider.BulkUploadStatus = bustatus;

            var result = await _providerService.UpdateProviderDetails(provider);
            if(result.IsFailure)
            {
                log.LogError($"Failed to update bulk upload status on provider {provider.ProviderName}");
                return false;
            }

            return true;
        }

    }
}
