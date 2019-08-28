using Dfc.CourseDirectory.Services;
using Dfc.CourseDirectory.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Integration.Helpers
{
    public static class LarsSearchServiceTestFactory
    {
        public static ILarsSearchService GetService()
        {
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<LarsSearchService>.Instance;
            var settings = TestConfig.GetSettings<LarsSearchSettings>("LarsSearchSettings");
            ILarsSearchService service = new LarsSearchService(logger, Options.Create(settings));
            return service;
        }
    }
}
