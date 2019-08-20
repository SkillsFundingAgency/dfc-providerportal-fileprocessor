using Dfc.CourseDirectory.Services.Interfaces.VenueService;
using Moq;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Unit.Mocks
{
    public static class VenueServiceMockFactory
    {
        public static IVenueService GetVenueService()
        {
            var mock = new Mock<IVenueService>();
            return mock.Object;
        }
    }
}
