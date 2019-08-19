using Dfc.CourseDirectory.Services.Interfaces;
using Moq;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Unit.Mocks
{
    public static class LarsSearchServiceMockFactory
    {
        public static ILarsSearchService GetLarsSearchService()
        {
            var mock = new Mock<ILarsSearchService>();
            return mock.Object;
        }
    }
}
