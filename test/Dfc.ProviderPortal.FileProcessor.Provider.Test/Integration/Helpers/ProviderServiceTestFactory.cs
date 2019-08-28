using Dfc.CourseDirectory.Services.Interfaces.ProviderService;
using Dfc.CourseDirectory.Services.ProviderService;
using Microsoft.Extensions.Options;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Integration.Helpers
{
    public static class ProviderServiceTestFactory
    {
        public static IProviderService GetService()
        {
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ProviderService>.Instance;
            var settings = TestConfig.GetSettings<ProviderServiceSettings>("ProviderServiceSettings");
            IProviderService service = new ProviderService(logger, new System.Net.Http.HttpClient(), Options.Create(settings));
            return service;
        }
    }
}
