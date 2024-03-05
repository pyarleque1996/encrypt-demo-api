using DEMO.Encrypt.API.Services.Encryption;
using DEMO.Encrypt.API.Services.Splitter;

namespace DEMO.Encrypt.API.Configuration;

public static class ConfigurationServices
{
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<ISplitterService, SplitterService>();
        services.AddScoped<ISevenZipEncryption, SevenZipEncryption>();

        return services;
    }
}
