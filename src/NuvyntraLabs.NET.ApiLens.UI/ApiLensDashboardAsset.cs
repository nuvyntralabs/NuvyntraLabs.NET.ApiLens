namespace NuvyntraLabs.NET.ApiLens.UI;

public static class ApiLensDashboardAsset
{
    public static Stream Open()
    {
        var assembly = typeof(ApiLensDashboardAsset).Assembly;
        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(resource => resource.EndsWith(".dashboard.html", StringComparison.Ordinal));
        if (name is null)
            throw new InvalidOperationException("ApiLens dashboard is missing from the package.");

        return assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("ApiLens dashboard is missing from the package.");
    }
}
