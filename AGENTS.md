# ApiLens — agent notes

ApiLens is the first NETEssentials product. It explains why an ASP.NET Core request was slow or failed.

- Package prefix: `NuvyntraLabs.NET.ApiLens.*`
- TFM: `net10.0`
- Core has no ASP.NET reference. EF and HTTP reference core only. `AddApiLens()` / `UseApiLens()` live on the ASP.NET Core package.
- Collection is `System.Diagnostics` (EF `DiagnosticListener`, HTTP `ActivityListener`). Do not add an OpenTelemetry dependency in this version.
- Explain percentages are wall-clock shares from `TimelineAttributor`. Do not sum inclusive durations into the total.
- Never read SQL parameter values, request bodies, response bodies, or HTTP query strings.
- Dashboard routes exist only when `EnableDashboard` is true and `IHostEnvironment.IsDevelopment()` is true.
- v1 hosts: MVC and minimal APIs. Do not add SignalR, gRPC, or Blazor circuit timing here.
- Redis, OpenTelemetry export, and AI stay unbuilt until a later version.
- Do not `dotnet nuget push` from a local clone.

Layout: `src/`, `tests/`, `samples/ApiLens.Sample`, `README.md`.
