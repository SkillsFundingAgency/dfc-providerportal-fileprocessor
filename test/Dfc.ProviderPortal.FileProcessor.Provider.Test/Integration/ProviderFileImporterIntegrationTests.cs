﻿using Dfc.CourseDirectory.Services.Interfaces;
using Dfc.CourseDirectory.Services.Interfaces.CourseService;
using Dfc.CourseDirectory.Services.Interfaces.ProviderService;
using Dfc.CourseDirectory.Services.Interfaces.VenueService;
using Dfc.CourseDirectory.Services.ProviderService;
using Dfc.ProviderPortal.FileProcessor.Provider.Test.Integration.Helpers;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Integration
{
    public class ProviderFileImporterIntegrationTests
    {
#if DEBUG   // hide integration test from release build (ie. Azure)
        [Fact]
#endif
        public void SetBulkUploadStatus_Should_Succeed()
        {
            // Arrange

            int ukPRN = 10003954;

            ILarsSearchService larsSearchService = LarsSearchServiceTestFactory.GetService();
            ICourseService courseService = CourseServiceTestFactory.GetService();
            IVenueService venueService = VenueServiceTestFactory.GetService();
            IProviderService providerService = ProviderServiceTestFactory.GetService();
            IProviderFileImporter importer = new ProviderCsvFileImporter(larsSearchService, courseService, venueService, providerService);
            var beforeProvider = providerService.GetProviderByPRNAsync(new ProviderSearchCriteria(ukPRN.ToString())).Result.Value.Value.First();
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            // Act

            var result = Task.Run(async () => await importer.SetBulkUploadStatus(logger, beforeProvider, 123)).Result;

            // Assert

            result.Should().BeTrue();
            var afterProvider = providerService.GetProviderByPRNAsync(new ProviderSearchCriteria(ukPRN.ToString())).Result.Value.Value.First();
            afterProvider.BulkUploadStatus.Should().NotBeNull();
            afterProvider.BulkUploadStatus.InProgress.Should().BeTrue();
        }

#if DEBUG   // hide integration test from release build (ie. Azure)
        [Fact]
#endif
        public void ClearBulkUploadStatus_Should_Succeed()
        {
            // Arrange

            int ukPRN = 10003954;

            ILarsSearchService larsSearchService = LarsSearchServiceTestFactory.GetService();
            ICourseService courseService = CourseServiceTestFactory.GetService();
            IVenueService venueService = VenueServiceTestFactory.GetService();
            IProviderService providerService = ProviderServiceTestFactory.GetService();
            IProviderFileImporter importer = new ProviderCsvFileImporter(larsSearchService, courseService, venueService, providerService);
            var beforeProvider = providerService.GetProviderByPRNAsync(new ProviderSearchCriteria(ukPRN.ToString())).Result.Value.Value.First();
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            // Act

            var result = Task.Run(async () => await importer.ClearBulkUploadStatus(logger, beforeProvider)).Result;

            // Assert

            result.Should().BeTrue();
            var afterProvider = providerService.GetProviderByPRNAsync(new ProviderSearchCriteria(ukPRN.ToString())).Result.Value.Value.First();
            afterProvider.BulkUploadStatus.Should().NotBeNull();
            afterProvider.BulkUploadStatus.InProgress.Should().BeFalse();
        }

        [Fact]
        public void DeleteCoursesForProviderTest()
        {
            // Arrange

            int ukPRN = 10003954;

            ILarsSearchService larsSearchService = LarsSearchServiceTestFactory.GetService();
            ICourseService courseService = CourseServiceTestFactory.GetService();
            IVenueService venueService = VenueServiceTestFactory.GetService();
            IProviderService providerService = ProviderServiceTestFactory.GetService();
            IProviderFileImporter importer = new ProviderCsvFileImporter(larsSearchService, courseService, venueService, providerService);
            var beforeProvider = providerService.GetProviderByPRNAsync(new ProviderSearchCriteria(ukPRN.ToString())).Result.Value.Value.First();
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            // Act

            var result = Task.Run(async () => await importer.DeleteCoursesForProvider(logger, ukPRN)).Result;

            // Assert

        }
    }
}
