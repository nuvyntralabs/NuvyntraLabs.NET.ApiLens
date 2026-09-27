# Nuvyntra ApiLens

Developer-first runtime diagnostics for ASP.NET Core. ApiLens answers why a request was slow or failed. It sits on `System.Diagnostics` (middleware, EF Core command events, `System.Net.Http` activities). It does not replace OpenTelemetry, ASP.NET Core metrics, or a production observability backend.

**GitHub:** https://github.com/nuvyntralabs/NuvyntraLabs.NET.ApiLens  
**Hub:** https://github.com/nuvyntralabs/NETEssentials  
**Author:** [Niladri Prasad Padhy](https://github.com/NiladriPadhy)  
**License:** MIT  
**Version:** 0.1.0  
**Target:** `net10.0`

`ApiLens` here is the Nuvyntra package prefix `NuvyntraLabs.NET.ApiLens`. It is not [endjin/ApiLens](https://github.com/endjin/ApiLens), the XML documentation search tool.

## Install

```bash
dotnet add package NuvyntraLabs.NET.ApiLens.AspNetCore
dotnet add package NuvyntraLabs.NET.ApiLens.EntityFrameworkCore
dotnet add package NuvyntraLabs.NET.ApiLens.Http
```

```csharp
builder.Services.AddApiLens();
builder.Services.AddApiLensEntityFrameworkCore();
builder.Services.AddApiLensHttp();
app.UseApiLens();
```

In Development, open `/_apilens`. The dashboard is not served in any other environment. `EnableDashboard = false` turns it off in Development as well. There is no option that turns it on in Production.

## What a request shows

Each request stores a timeline of endpoint time, EF Core commands, and outbound `HttpClient` calls. Explain percentages are a share of wall-clock time. Overlapping calls are counted once, on the longer call, and the timeline still lists every call with its own duration. A repeated SQL shape is reported as a possible N+1, not as a proven missing `Include`.

## Capture

| | Development | Production |
| --- | --- | --- |
| Dashboard | On, unless `EnableDashboard` is false | Off |
| SQL text | Stored after literal redaction | Dropped after grouping |
| Parameter values | Never read | Never read |
| Request and response bodies | Never stored | Never stored |
| HTTP query strings | Not stored (host and status only) | Not stored |
| Exception text | Type name only | Type name only |

Set `ApiLensOptions.Capture` only when the host environment is the wrong signal. Leaving it null follows the environment.

## Packages

| Package | Role |
| --- | --- |
| `NuvyntraLabs.NET.ApiLens` | Timeline, attribution, N+1, explain rules, in-memory ring. No ASP.NET reference. |
| `NuvyntraLabs.NET.ApiLens.AspNetCore` | `AddApiLens` / `UseApiLens` for MVC and minimal APIs, plus the dashboard. |
| `NuvyntraLabs.NET.ApiLens.EntityFrameworkCore` | EF Core `CommandExecuted` / `CommandError` on the active request. |
| `NuvyntraLabs.NET.ApiLens.Http` | Outbound `HttpClient` timings on the active request. Factory clients use a handler. Other clients use the `System.Net.Http` diagnostic listener. |
| `NuvyntraLabs.NET.ApiLens.UI` | Embedded dashboard page. Referenced by the ASP.NET Core package. |

Redis, OpenTelemetry export, and an AI explain layer are later packages in this repo. They are not part of 0.1.0.

## Sample

```bash
dotnet run --project samples/ApiLens.Sample
```

`GET /api/orders` runs a deliberate per-row query loop. The dashboard is at `/_apilens`.

## Out of this version

SignalR, gRPC, and Blazor circuits. Work that leaves the request (`Channel`, Hangfire, a background queue) is not attributed. Per-middleware names and JSON serialization time are omitted: ASP.NET Core does not expose them as reliable spans. DNS / connect / TLS splits are omitted unless a later HTTP package can read them from `SocketsHttpHandler` without guessing.

Publishing is pipeline-only. Do not `dotnet nuget push` from a local clone. CI packs `net10.0` and pushes nupkg and snupkg to nuget.org (`NUGET_KEY_APILENS`) and GitHub Packages.
