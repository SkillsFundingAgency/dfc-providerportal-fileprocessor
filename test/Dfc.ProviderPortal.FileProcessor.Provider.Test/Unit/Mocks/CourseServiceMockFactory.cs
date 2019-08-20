using Dfc.CourseDirectory.Services.Interfaces.CourseService;
using Moq;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Unit.Mocks
{
    public static class CourseServiceMockFactory
    {
        public static ICourseService GetCourseService()
        {
            var mock = new Mock<ICourseService>();
            return mock.Object;
        }
    }
}
