using DEMO.Encrypt.API.Services.Encryption;
using DEMO.Encrypt.API.Services.Splitter;

namespace DEMO.Encrypt.API.Configuration;

public static class ConfigurationServices
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IExporterService, ExporterService>();
        services.AddScoped<ISevenZipEncryption, SevenZipEncryption>();

        return services;
    }
}
