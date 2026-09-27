using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace NuvyntraLabs.NET.ApiLens.AspNetCore;

public static class ApiLensServiceCollectionExtensions
{
    public static IServiceCollection AddApiLens(this IServiceCollection services, Action<ApiLensOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<ApiLensOptions>();
        if (configure is not null)
            services.Configure(configure);

        services.TryAddSingleton<IApiLensStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ApiLensOptions>>().Value;
            return new InMemoryApiLensStore(options.MaxStoredRequests);
        });

        return services;
    }
}
