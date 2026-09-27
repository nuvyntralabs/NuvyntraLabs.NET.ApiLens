using Microsoft.AspNetCore.Builder;

namespace NuvyntraLabs.NET.ApiLens.AspNetCore;

public static class ApiLensApplicationBuilderExtensions
{
    public const string DashboardPath = "/_apilens";

    public static IApplicationBuilder UseApiLens(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ApiLensMiddleware>();
    }
}
