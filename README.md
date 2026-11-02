# Engineering Playground

A collection of practical .NET examples exploring software architecture, design decisions, and engineering trade-offs.

The goal is to experiment with different approaches, understand their strengths and limitations, and share what I learn along the way.

## Order Processing — Simple API vs. Clean Architecture

This solution contains two implementations of an Order Processing API built with ASP.NET Core, Entity Framework Core, and SQLite.

Both implementations demonstrate the same core business requirements:

- Create orders using product prices stored in the database.
- Apply a 10% discount when the order subtotal exceeds €200.
- Allow cancellation only while an order is pending.

### Simple API

`OrderProcessing.SimpleApi`

A straightforward ASP.NET Core Minimal API where HTTP endpoints coordinate validation, business rules, and persistence using Entity Framework Core directly.

### Clean Architecture

The second implementation separates responsibilities across four projects:

- `OrderProcessing.Domain` — order entities and business rules.
- `OrderProcessing.Application` — use cases and application contracts.
- `OrderProcessing.Infrastructure` — EF Core persistence and repository implementations.
- `OrderProcessing.Api` — HTTP endpoints and dependency injection configuration.

Three additional test projects cover domain behavior, application use cases, and infrastructure integration.

## Running the Examples

**Requirements:** .NET 10 SDK.

From the repository root, restore and build the solution:

```bash
dotnet restore
dotnet build
```

Run the Simple API:

```bash
dotnet run --project OrderProcessing.SimpleApi
```

Or run the Clean Architecture API:

```bash
dotnet run --project OrderProcessing.Api
```

The APIs use SQLite. The Clean Architecture implementation includes EF Core migrations and demonstration products.

To run the automated tests:

```bash
dotnet test
```

## About This Repository

These implementations are intentionally small. Their purpose is to illustrate architectural decisions and trade-offs rather than provide a production-ready order management system.

The examples accompany my article, *Clean Architecture in .NET: When Does It Make Sense?*