using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NuvyntraLabs.NET.ApiLens.AspNetCore;
using NuvyntraLabs.NET.ApiLens.EntityFrameworkCore;
using NuvyntraLabs.NET.ApiLens.Http;

var builder = WebApplication.CreateBuilder(args);

var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
builder.Services.AddSingleton(connection);
builder.Services.AddDbContext<OrdersDb>(options => options.UseSqlite(connection));
builder.Services.AddApiLens();
builder.Services.AddApiLensEntityFrameworkCore();
builder.Services.AddApiLensHttp();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDb>();
    db.Database.EnsureCreated();
    if (!db.Orders.Any())
    {
        for (var index = 0; index < 12; index++)
            db.Orders.Add(new Order { CustomerName = "Customer " + index });
        db.SaveChanges();
    }
}

app.UseApiLens();
app.MapGet("/", () => Results.Redirect("/_apilens"));
app.MapGet("/api/ping", () => Results.Ok(new { ok = true }));
app.MapGet("/api/orders", async (OrdersDb db) =>
{
    var orders = await db.Orders.AsNoTracking().ToListAsync();
    var names = new List<string>(orders.Count);
    foreach (var order in orders)
    {
        var name = await db.Orders.AsNoTracking()
            .Where(row => row.Id == order.Id)
            .Select(row => row.CustomerName)
            .FirstAsync();
        names.Add(name);
    }

    return Results.Ok(names);
});

app.Run();

sealed class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
}

sealed class OrdersDb(DbContextOptions<OrdersDb> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}
