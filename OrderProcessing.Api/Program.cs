using Microsoft.EntityFrameworkCore;
using OrderProcessing.Api.Contracts;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Models;
using OrderProcessing.Application.Results;
using OrderProcessing.Application.UseCases;
using OrderProcessing.Infrastructure.Catalog;
using OrderProcessing.Infrastructure.Persistence;
using OrderProcessing.Infrastructure.Persistence.Models;
using OrderProcessing.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

// Infrastructure
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Orders")));

// Application contracts
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductCatalog, ProductCatalog>();

// Application use cases
builder.Services.AddScoped<CreateOrder>();
builder.Services.AddScoped<CancelOrder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

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

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    Message = "Order Processing Clean Architecture API is running"
}));

app.MapPost("/orders", async (
    CreateOrderRequest request,
    CreateOrder createOrder,
    CancellationToken cancellationToken) =>
{
    if (request.Items is null || request.Items.Count == 0)
    {
        return Results.BadRequest(
            "Order must contain at least one item.");
    }

    try
    {
        var items = request.Items
            .Select(item => new CreateOrderItem(
                item.ProductId,
                item.Quantity))
            .ToArray();

        var order = await createOrder.ExecuteAsync(
            items,
            cancellationToken);

        return Results.Created($"/orders/{order.Id}", new
        {
            order.Id,
            order.Subtotal,
            order.Discount,
            order.Total
        });
    }
    catch (KeyNotFoundException)
    {
        return Results.BadRequest(
            "One or more products do not exist.");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapPost("/orders/{id:int}/cancel", async (
    int id,
    CancelOrder cancelOrder,
    CancellationToken cancellationToken) =>
{
    var result = await cancelOrder.ExecuteAsync(
        id,
        cancellationToken);

    return result switch
    {
        CancelOrderResult.Success =>
            Results.Ok(new
            {
                Id = id,
                Status = "Cancelled"
            }),

        CancelOrderResult.NotFound =>
            Results.NotFound(
                $"Order with ID {id} was not found."),

        CancelOrderResult.AlreadyCancelled =>
            Results.Conflict(
                "Only pending orders can be cancelled."),

        _ => Results.StatusCode(500)
    };
});

app.Run();