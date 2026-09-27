using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace NuvyntraLabs.NET.ApiLens.EntityFrameworkCore;

public static class ApiLensEntityFrameworkCoreExtensions
{
    public static IServiceCollection AddApiLensEntityFrameworkCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EfCommandListener>());
        return services;
    }
}
