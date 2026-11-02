using Microsoft.EntityFrameworkCore;
using OrderProcessing.SimpleApi.Contracts;
using OrderProcessing.SimpleApi.Data;
using OrderProcessing.SimpleApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=orders.db"));

var app = builder.Build();

// Seed sample products for local development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    if (!await db.Products.AnyAsync())
    {
        db.Products.AddRange(
            new Product
            {
                Name = "Mechanical Keyboard",
                Price = 120.00m
            },
            new Product
            {
                Name = "Wireless Mouse",
                Price = 85.00m
            });

        await db.SaveChangesAsync();
    }
}

// Configure the HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Our first Minimal API endpoint
app.MapGet("/", () => Results.Ok(new
{
    Message = "Order Processing API is running"
}));


app.MapPost("/orders", async (
    CreateOrderRequest request,
    AppDbContext db) =>
{
    if (request.Items is null || request.Items.Count == 0)
    {
        return Results.BadRequest("Order cannot be empty.");
    }

    if (request.Items.Any(item => item.Quantity <= 0))
    {
        return Results.BadRequest("Quantity must be positive.");
    }

    var productIds = request.Items
        .Select(item => item.ProductId)
        .Distinct()
        .ToArray();

    if (productIds.Length != request.Items.Count)
    {
        return Results.BadRequest("Duplicate products are not allowed.");
    }

    var products = await db.Products
        .Where(product => productIds.Contains(product.Id))
        .ToDictionaryAsync(product => product.Id);

    if (products.Count != productIds.Length)
    {
        return Results.BadRequest("One or more products do not exist.");
    }

    var order = new Order
    {
        CreatedAtUtc = DateTime.UtcNow
    };

    foreach (var item in request.Items)
    {
        var product = products[item.ProductId];

        order.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            Quantity = item.Quantity,
            UnitPrice = product.Price
        });
    }


    order.Subtotal = order.Items.Sum(
        item => item.UnitPrice * item.Quantity);

    order.Discount = order.Subtotal > 200m
        ? decimal.Round(
            order.Subtotal * 0.10m,
            2,
            MidpointRounding.AwayFromZero)
        : 0m;

    order.Total = order.Subtotal - order.Discount;


    db.Orders.Add(order);
    await db.SaveChangesAsync();


    return Results.Created($"/orders/{order.Id}", new
    {
        order.Id,
        order.Subtotal,
        order.Discount,
        order.Total
    });
});

app.MapPost("/orders/{id:int}/cancel", async (
    int id,
    AppDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);

    if (order is null)
    {
        return Results.NotFound(
            $"Order with ID {id} was not found.");
    }

    if (order.Status != OrderStatus.Pending)
    {
        return Results.Conflict(
            "Only pending orders can be cancelled.");
    }

    order.Status = OrderStatus.Cancelled;

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        order.Id,
        order.Status
    });
});

app.Run();
