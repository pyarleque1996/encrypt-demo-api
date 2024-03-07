using DEMO.Encrypt.API.Services.Compression;
using DEMO.Encrypt.API.Services.Splitter;

namespace DEMO.Encrypt.API.Configuration;

public static class ConfigurationServices
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IExporterService, ExporterService>();
        services.AddScoped<ISevenZipCompression, SevenZipCompression>();

        return services;
    }
}
