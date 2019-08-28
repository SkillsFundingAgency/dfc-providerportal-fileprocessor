using Microsoft.Extensions.Configuration;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Integration.Helpers
{
    public class TestConfig
    {
        private static IConfigurationRoot GetIConfigurationRoot(string outputPath)
        {
            return new ConfigurationBuilder()
                .SetBasePath(outputPath)
                .AddJsonFile("appsettings.Test.json", optional: true)                
                .Build();
        }

        public static T GetSettings<T>(string sectionName) where T: new()
        {
            var settings = new T();
            var iConfig = GetIConfigurationRoot(System.Environment.CurrentDirectory + @"\Integration\Helpers");

            iConfig
                .GetSection(sectionName)
                .Bind(settings);

            return settings;
        }
    }
}
