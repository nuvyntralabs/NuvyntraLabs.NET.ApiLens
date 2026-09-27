namespace NuvyntraLabs.NET.ApiLens;

public sealed class ApiLensOptions
{
    public TimeSpan SlowRequestThreshold { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Same SQL shape at least this many times in one request is reported as a possible N+1.</summary>
    public int NPlusOneRepeatThreshold { get; set; } = 8;

    public int MaxStoredRequests { get; set; } = 200;

    /// <summary>
    /// Null follows the host: Development keeps redacted SQL text, every other environment keeps timings only.
    /// </summary>
    public ApiLensCaptureMode? Capture { get; set; }

    /// <summary>
    /// The dashboard is served only when this is true and the host environment is Development.
    /// There is no switch that turns the dashboard on outside Development.
    /// </summary>
    public bool EnableDashboard { get; set; } = true;

    public ApiLensCaptureMode ResolveCapture(bool isDevelopment)
        => Capture ?? (isDevelopment ? ApiLensCaptureMode.Development : ApiLensCaptureMode.Production);
}
