using CsvHelper.Configuration;
using Dfc.CourseDirectory.Services;
using Dfc.CourseDirectory.Services.ApprenticeshipService;
using Dfc.CourseDirectory.Services.CourseService;
using Dfc.CourseDirectory.Services.Interfaces;
using Dfc.CourseDirectory.Services.Interfaces.ApprenticeshipService;
using Dfc.CourseDirectory.Services.Interfaces.CourseService;
using Dfc.CourseDirectory.Services.Interfaces.ProviderService;
using Dfc.CourseDirectory.Services.Interfaces.VenueService;
using Dfc.CourseDirectory.Services.ProviderService;
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

            builder.Services.Configure<ProviderServiceSettings>(configuration.GetSection(nameof(ProviderServiceSettings)));
            builder.Services.AddScoped<IProviderService, ProviderService>();

            builder.Services.AddScoped<IProviderFileImporter, ProviderCsvFileImporter>();

            builder.Services.Configure<IApprenticeshipService>(configuration.GetSection(nameof(ApprenticeshipService)));
            builder.Services.AddScoped<IApprenticeshipService, ApprenticeshipService>();
            builder.Services.AddScoped<IApprenticeshipServiceSettings, ApprenticeshipServiceSettings>();

            builder.Services.Configure<ApprenticeshipServiceSettings>(configuration.GetSection(nameof(ApprenticeshipServiceSettings)));
            builder.Services.AddScoped<IApprenticeshipBulkUploadService, ApprenticeshipBulkUploadService>();
        }
    }
}
