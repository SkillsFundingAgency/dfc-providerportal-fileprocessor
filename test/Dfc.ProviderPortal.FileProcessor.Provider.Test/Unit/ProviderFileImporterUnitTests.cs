using Dfc.CourseDirectory.Services.Interfaces;
using Dfc.CourseDirectory.Services.Interfaces.CourseService;
using Dfc.CourseDirectory.Services.Interfaces.ProviderService;
using Dfc.CourseDirectory.Services.Interfaces.VenueService;
using Dfc.ProviderPortal.FileProcessor.Provider.Test.Unit.Helpers;
using Dfc.ProviderPortal.FileProcessor.Provider.Test.Unit.Mocks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Unit
{
    public class ProviderFileImporterUnitTests
    {
        public class ParseCsvFile
        {
            [Fact]
            public void When_File_Is_ValidCsv_Then_File_Should_Import()
            {
                // Arrange

                ILarsSearchService larsSearchService = LarsSearchServiceMockFactory.GetLarsSearchService();
                ICourseService courseService = CourseServiceMockFactory.GetCourseService();
                IVenueService venueService = VenueServiceMockFactory.GetVenueService();
                IProviderService providerService = ProviderServiceMockFactory.GetProviderService();
                IProviderFileImporter importer = new ProviderCsvFileImporter(larsSearchService, courseService, venueService, providerService);
                Stream fileStream = CsvStreams.BulkUpload_ValidMultiple();
                ILogger log = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                int ukPRN = 0;

                // Act

                List<string> errors;
                var courses = importer.ParseCsvFile(log, @"10000020\Bulk Upload\Files\190627-082122 Provider Name Ltd.csv", fileStream, ukPRN, out errors);
                fileStream.Close();

                // Assert

                errors.Should().NotBeNull();
                errors.Should().BeEmpty();
            }
        }
    }
}
