using Dfc.CourseDirectory.Services;
using Dfc.CourseDirectory.Services.CourseService;
using Dfc.CourseDirectory.Services.Interfaces;
using Dfc.CourseDirectory.Services.Interfaces.CourseService;
using Dfc.CourseDirectory.Services.Interfaces.VenueService;
using Dfc.CourseDirectory.Services.VenueService;
using Dfc.ProviderPortal.FileProcessor.Provider;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Dfc.ProviderPortal.FileProcessor.Functions.Startup))]

namespace Dfc.ProviderPortal.FileProcessor.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            builder.Services.AddSingleton<IConfiguration>(configuration);

            builder.Services.Configure<VenueServiceSettings>(configuration.GetSection(nameof(VenueServiceSettings)));
            builder.Services.AddScoped<IVenueService, VenueService>();

            builder.Services.Configure<LarsSearchSettings>(configuration.GetSection(nameof(LarsSearchSettings)));
            builder.Services.AddScoped<ILarsSearchService, LarsSearchService>();

            builder.Services.Configure<CourseServiceSettings>(configuration.GetSection(nameof(CourseServiceSettings)));
            builder.Services.Configure<FindACourseServiceSettings>(configuration.GetSection(nameof(FindACourseServiceSettings)));
            builder.Services.AddScoped<ICourseService, CourseService>();

            builder.Services.AddScoped<IProviderFileImporter, ProviderCsvFileImporter>();
        }
    }
}
