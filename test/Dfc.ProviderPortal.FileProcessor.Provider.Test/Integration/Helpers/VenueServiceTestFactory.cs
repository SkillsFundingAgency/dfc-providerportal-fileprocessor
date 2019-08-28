using Dfc.CourseDirectory.Services.Interfaces.VenueService;
using Dfc.CourseDirectory.Services.VenueService;
using Microsoft.Extensions.Options;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Integration.Helpers
{
    public static class VenueServiceTestFactory
    {
        public static IVenueService GetService()
        {
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<VenueService>.Instance;
            var settings = TestConfig.GetSettings<VenueServiceSettings>("VenueServiceSettings");
            IVenueService service = new VenueService(logger, Options.Create(settings));
            return service;
        }
    }
}
