using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinkShortener.Api.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // TryAdd permite que os testes troquem o relógio e o DNS antes desta chamada.
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IHostAddressResolver, DnsHostAddressResolver>();
        services.TryAddSingleton<ICodeGenerator, RandomCodeGenerator>();

        services.AddMemoryCache(options => options.SizeLimit = LinkResolutionCache.MaxEntries);
        services.AddSingleton<LinkResolutionCache>();
        services.AddSingleton<TargetUrlValidator>();
        services.AddScoped<LinkService>();

        return services;
    }
}
