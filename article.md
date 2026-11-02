# Clean Architecture in .NET: When Does It Make Sense?

Clean Architecture is a common topic in the .NET community, and for good reason. Separating business logic from infrastructure concerns can make an application easier to understand, test, and maintain.

But does every ASP.NET Core application really need multiple projects, layers, and interfaces?

Let's say we're building a small Order Processing API. Customers can place orders, the application validates the items, calculates the total, and saves everything to a database.

A Minimal API endpoint using Entity Framework Core seems like a reasonable place to start. The requirements are straightforward, and introducing additional architectural layers might not provide much value yet.

But what happens when those requirements change?

We might need to introduce discount rules, handle order cancellations, or support more complex business workflows. Should we immediately move to Clean Architecture, or would a simpler approach still be enough?

In this article, we'll explore that decision using a practical ASP.NET Core example. We'll start with a straightforward implementation, introduce additional business rules, and examine what we gain — and what we give up — when we introduce Clean Architecture.

You can find the complete source code for both implementations in the [Engineering Playground repository](https://github.com/NGeorgievKolev/engineering-playground) on GitHub.

## Starting with a Simple API

Our initial requirements are straightforward. A customer can create an order containing one or more products. The application needs to validate the request, retrieve the product prices, calculate the total amount, and save the order to a database.

For this scenario, a Minimal API endpoint that works directly with Entity Framework Core seems like a reasonable starting point. We don't necessarily need separate application and infrastructure projects or additional abstractions.

Of course, keeping things simple doesn't mean ignoring code quality. We still need proper validation and code that's easy to understand and change.

Here's a simplified version of our order creation endpoint:

```csharp
app.MapPost("/orders", async (
    CreateOrderRequest request,
    AppDbContext db) =>
{
    // Validate the request and load products...

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

    order.Total = order.Items.Sum(
        item => item.UnitPrice * item.Quantity);

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Created($"/orders/{order.Id}", new
    {
        order.Id,
        order.Total
    });
});
```

The endpoint handles the order creation process, from validating the request to calculating the total and saving the order.

Notice that we're injecting `AppDbContext` directly into the endpoint. There's no repository interface, application service, or separate infrastructure layer involved.

For a small application with limited business rules, I think this is a reasonable approach. We can follow the entire operation without jumping between multiple files or projects.

The trade-off is that our endpoint is responsible for more than handling HTTP requests. It also contains logic related to order creation and persistence.

That's manageable while the requirements are simple. But as we introduce more business rules, it's worth asking whether those responsibilities should continue to live together.

## When Complexity Grows

Our Order Processing API is working well, and the initial implementation is easy enough to follow.

Now let's introduce two additional requirements.

Orders with a subtotal above €200 should receive a 10% discount. Customers should also be able to cancel their orders, but only while an order is still pending.

Neither requirement is particularly difficult to implement. We could add a few conditions to our existing endpoints, and for an application of this size, that might still be a perfectly reasonable solution.

The challenge appears when business rules start changing or interacting with one another. Discount policies might become more complicated, order statuses might introduce additional restrictions, and the same rules might need to be applied in different parts of the application.

Keeping everything inside HTTP endpoints can then become harder to manage.

But that doesn't automatically mean we need Clean Architecture. Before introducing additional layers, it's worth looking at which responsibilities would benefit from being separated.

### Business Rules Inside the Endpoint

With these requirements in place, our API now handles more than basic order creation.

The `POST /orders` endpoint calculates a discount based on the order subtotal, while the cancellation endpoint checks whether an order is still pending before updating its status.

Here's how the discount calculation looks in our simple implementation:

```csharp
order.Discount = order.Subtotal > 200m
    ? decimal.Round(
        order.Subtotal * 0.10m,
        2,
        MidpointRounding.AwayFromZero)
    : 0m;

order.Total = order.Subtotal - order.Discount;
```

There's nothing inherently wrong with keeping this logic in the endpoint. But if the discount policy changes or needs to be reused, we'd have to revisit our HTTP request-handling code.

The cancellation rule raises a similar question. Whether an order can be cancelled is a business decision, even if we're currently checking it inside an endpoint.

This is where I'd consider moving business rules away from request handling.

That doesn't necessarily mean introducing Clean Architecture. A small application service or a method on the domain entity might be enough.

The goal is to keep the code understandable as the requirements evolve, without adding more structure than the application needs.

## Introducing Clean Architecture

Our Order Processing API now has a few business rules, but that doesn't mean we need to restructure the entire application.

We could move the discount calculation into a service, keep the cancellation rule inside the `Order` entity, and still have a perfectly reasonable single-project application.

So what would Clean Architecture add?

For me, the interesting part is not the number of projects or interfaces. It's the way we organize dependencies so that business logic doesn't need to depend on the frameworks and infrastructure around it.

Let's see what that looks like in our Order Processing API.

### The Dependency Rule

One of the central ideas behind Clean Architecture is that dependencies should point inward, toward the business logic.

In our implementation, the Domain contains the entities and rules that describe how orders behave. It doesn't reference ASP.NET Core, Entity Framework Core, or SQLite.

The Application layer coordinates operations such as creating and cancelling orders. It depends on the Domain and defines interfaces for the external capabilities it needs, including order persistence and product pricing.

Infrastructure implements those interfaces using Entity Framework Core and SQLite. The API handles incoming HTTP requests, calls the application use cases, and translates their results into HTTP responses.

The important detail is that the Application layer doesn't need to know which database we're using. Infrastructure depends on the application contracts, not the other way around.

This gives us a clear place for business rules and makes it possible to test them without involving the HTTP pipeline or a database.

Of course, these boundaries come with additional code and configuration. Simply splitting a project into multiple assemblies doesn't guarantee better architecture.

### Moving Business Rules into the Domain

Let's look at order cancellation.

In our original Minimal API, we checked the order status directly inside the endpoint. That worked, but the rule was part of the HTTP request-handling code.

In the Clean Architecture version, the `Order` entity is responsible for deciding whether it can be cancelled:

```csharp
public void Cancel()
{
    if (Status != OrderStatus.Pending)
    {
        throw new InvalidOperationException(
            "Only pending orders can be cancelled.");
    }

    Status = OrderStatus.Cancelled;
}
```

Now the order protects its own state. The cancellation rule doesn't depend on HTTP status codes or database operations.

We can also test it directly. A pending order can be cancelled, while attempting to cancel an already cancelled order throws an exception.

I think this is a meaningful improvement, but it's worth pointing out that we don't need four separate projects to achieve it. A domain method like this can work just as well in a small, single-project application.

Clean Architecture becomes more interesting when we want to preserve these boundaries consistently as the application grows.

### Coordinating Operations in the Application Layer

The Domain decides whether an order can be cancelled, but it doesn't need to know how orders are loaded or saved.

That's where the Application layer comes in.

Our `CancelOrder` use case depends on an `IOrderRepository` interface rather than working directly with Entity Framework Core.

Here's the implementation:

```csharp
public async Task<CancelOrderResult> ExecuteAsync(
    int orderId,
    CancellationToken cancellationToken = default)
{
    var order = await repository.GetByIdAsync(
        orderId,
        cancellationToken);

    if (order is null)
        return CancelOrderResult.NotFound;

    if (order.Status == OrderStatus.Cancelled)
        return CancelOrderResult.AlreadyCancelled;

    order.Cancel();

    await repository.SaveChangesAsync(cancellationToken);

    return CancelOrderResult.Success;
}
```

The use case coordinates the operation: it retrieves the order, handles expected outcomes, asks the domain entity to perform the cancellation, and saves the result.

The business rule still lives in `Order.Cancel()`. The use case checks for an already cancelled order so it can return an explicit result, while the domain entity remains responsible for protecting its own state.

The API can then translate `Success`, `NotFound`, or `AlreadyCancelled` into the appropriate HTTP response without making the Application layer depend on HTTP concepts.

We can test this use case with a fake repository, and the Domain tests can verify the cancellation rule independently.

But we should also acknowledge what we've added: an interface, a use case, a result type, dependency injection configuration, and separate projects.

For a small API, that might be more structure than we need. For an application with growing workflows and changing business rules, the separation may become much more valuable.

## When Is Clean Architecture Worth the Complexity?

We've now implemented the same core order processing functionality using two different approaches.

The first uses a straightforward Minimal API with Entity Framework Core. The second separates the application into Domain, Application, Infrastructure, and API layers.

Both can create orders, apply discounts, and handle cancellations. So what did we actually gain by introducing Clean Architecture?

The biggest difference is where responsibilities live.

In the simple implementation, the endpoints handle HTTP requests, business decisions, and database operations. Everything is close together, which makes the application easy to follow when the requirements are small.

In the Clean Architecture version, those responsibilities are separated. The Domain owns the business rules, the Application layer coordinates operations, and Infrastructure handles persistence.

This makes it easier to reason about business behavior independently of HTTP and database concerns. We also tested the Domain and Application layers in isolation, along with integration tests for the EF Core implementation.

But we shouldn't ignore the cost.

What started as a single ASP.NET Core project has grown into multiple projects, interfaces, use cases, repository implementations, and dependency injection configuration.

That's a considerable amount of structure for an API that creates and cancels orders.

If I were building a small internal API with a few stable business rules, I probably wouldn't start with the full Clean Architecture setup. A well-organized Minimal API, perhaps with a small application service and some domain methods, would likely be enough.

On the other hand, if the application were expected to support more complex workflows, frequently changing business policies, or multiple ways of interacting with the same business logic, I would be more inclined to introduce explicit architectural boundaries.

Clean Architecture doesn't automatically make an application better. It gives us a way to manage certain kinds of complexity, but the value depends on the problems we're actually solving.

## Final Thoughts

I think Clean Architecture is a valuable approach, but it's not something I would introduce automatically into every .NET application.

As we've seen with our Order Processing API, a simple implementation can be perfectly reasonable when the requirements are straightforward. Adding more layers doesn't necessarily make the code better.

What matters is understanding when the current structure starts getting in the way.

If business rules become harder to maintain, responsibilities start overlapping, or testing requires too much setup, introducing clearer boundaries might be worth the additional complexity.

But if the application is small, easy to understand, and simple to change, there's nothing wrong with keeping it that way.

For me, good architecture isn't about following a particular template. It's about making decisions that fit the problem we're trying to solve.

And sometimes, the simplest solution really is the right one.