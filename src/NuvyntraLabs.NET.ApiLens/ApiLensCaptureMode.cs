namespace NuvyntraLabs.NET.ApiLens;

public enum ApiLensCaptureMode
{
    /// <summary>Redacted SQL text and dependency names. Parameter values and request bodies are never stored.</summary>
    Development = 0,

    /// <summary>Durations, counts, status codes, and trace ids. SQL text is dropped after N+1 grouping.</summary>
    Production = 1
}
