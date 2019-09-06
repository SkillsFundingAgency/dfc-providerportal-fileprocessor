using Dfc.CourseDirectory.Services.Interfaces.ProviderService;
using Moq;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Unit.Mocks
{
    public static class ProviderServiceMockFactory
    {
        public static IProviderService GetProviderService()
        {
            var mock = new Mock<IProviderService>();
            return mock.Object;
        }
    }
}
