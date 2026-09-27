using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NuvyntraLabs.NET.ApiLens.EntityFrameworkCore;
using NuvyntraLabs.NET.ApiLens.Http;

namespace NuvyntraLabs.NET.ApiLens.AspNetCore.Tests;

public class DashboardTests
{
    [Fact]
    public async Task Development_dashboard_records_mvc_and_minimal_routes_without_bodies()
    {
        await using var app = await Host.StartAsync("Development", static builder =>
        {
            builder.Services.AddApiLens();
        });
        app.MapPost("/api/orders/{id}", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            return Results.Ok(new { length = body.Length });
        });
        await app.StartAsync();

        using var client = Host.Client(app);
        var page = await client.GetAsync("/_apilens");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Nuvyntra ApiLens", await page.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var posted = await client.PostAsync("/api/orders/15", new StringContent("secret-body"));
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);

        var json = await client.GetStringAsync("/_apilens/api/requests");
        Assert.Contains("/api/orders/{id}", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-body", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dashboard_is_absent_outside_development()
    {
        await using var app = await Host.StartAsync("Production");
        app.MapGet("/api/ping", () => Results.Ok());
        await app.StartAsync();

        using var client = Host.Client(app);
        var dashboard = await client.GetAsync("/_apilens");
        var ping = await client.GetAsync("/api/ping");

        Assert.Equal(HttpStatusCode.NotFound, dashboard.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ping.StatusCode);
    }

    [Fact]
    public async Task Ef_and_http_timings_show_up_on_the_request()
    {
        SqliteConnection? connection = null;
        await using var app = await Host.StartAsync("Development", builder =>
        {
            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            builder.Services.AddSingleton(connection);
            builder.Services.AddDbContext<OrdersDb>(options => options.UseSqlite(connection));
            builder.Services.AddApiLens();
            builder.Services.AddApiLensEntityFrameworkCore();
            builder.Services.AddApiLensHttp();
            builder.Services.AddHttpClient("demo")
                .ConfigurePrimaryHttpMessageHandler(() => new StubHandler());
        });

        app.MapGet("/api/orders", async (OrdersDb db, IHttpClientFactory http) =>
        {
            var orders = await db.Orders.AsNoTracking().ToListAsync();
            foreach (var order in orders)
            {
                _ = await db.Orders.AsNoTracking().Where(row => row.Id == order.Id).Select(row => row.CustomerName).FirstAsync();
            }

            var client = http.CreateClient("demo");
            var response = await client.GetAsync("http://inventory.test/pay?token=secret");
            return Results.Ok(new { orders.Count, status = (int)response.StatusCode });
        });

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrdersDb>();
            db.Database.EnsureCreated();
            for (var index = 0; index < 8; index++)
                db.Orders.Add(new Order { CustomerName = "Ada" });
            db.SaveChanges();
        }

        await app.StartAsync();
        using var client = Host.Client(app);
        var response = await client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await client.GetStringAsync("/_apilens/api/requests"));
        var id = document.RootElement[0].GetProperty("id").GetGuid();
        var detail = await client.GetStringAsync("/_apilens/api/requests/" + id);
        using var detailDocument = JsonDocument.Parse(detail);
        var statements = detailDocument.RootElement.GetProperty("statements").EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("inventory.test", detail, StringComparison.Ordinal);
        Assert.Contains(statements, statement => statement is not null && statement.Contains("Possible N+1", StringComparison.Ordinal));
        Assert.DoesNotContain("token=secret", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Ada", detail, StringComparison.Ordinal);
        connection?.Dispose();
    }

    private sealed class OrdersDb(DbContextOptions<OrdersDb> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();
    }

    private sealed class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private static class Host
    {
        public static Task<WebApplication> StartAsync(string environment, Action<WebApplicationBuilder>? configure = null)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = environment,
                ApplicationName = "ApiLens.Tests"
            });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            configure?.Invoke(builder);
            builder.Services.AddApiLens();
            var app = builder.Build();
            app.UseApiLens();
            return Task.FromResult(app);
        }

        public static HttpClient Client(WebApplication app)
            => new() { BaseAddress = new Uri(app.Urls.First()) };
    }
}
